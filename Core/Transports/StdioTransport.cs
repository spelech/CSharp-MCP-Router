using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using McpRouter.Core.Secrets;
using McpRouter.Models;
using Microsoft.Extensions.Logging;

namespace McpRouter.Core.Transports
{
    public class StdioTransport : ITransport
    {
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(15);
        private readonly McpServer _server;
        private readonly ILogger _logger;
        private readonly JsonRpcStateManager _stateManager;
        private readonly ISecretRetriever? _secretRetriever;
        private readonly CancellationTokenSource _cts = new();
        private readonly SemaphoreSlim _writeLock = new(1, 1);

        private System.Diagnostics.Process? _process;
        private Task? _readerTask;
        private Task? _stderrTask;
        private int _exitHandled = 0;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            Converters = { new JsonRpcMessageConverter() }
        };

        public StdioTransport(McpServer server, ILogger logger, JsonRpcStateManager stateManager, ISecretRetriever? secretRetriever = null)
        {
            _server = server;
            _logger = logger;
            _stateManager = stateManager;
            _secretRetriever = secretRetriever;
        }

        public async Task<string?> ResolveTokenAsync(ISecretRetriever? secretRetriever = null)
        {
            var provider = _server.SecretProvider ?? "None";
            if (provider.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                return !string.IsNullOrEmpty(_server.ApiKey) ? _server.ApiKey : null;
            }

            var retriever = secretRetriever ?? _secretRetriever;
            if (retriever == null)
            {
                throw new InvalidOperationException($"SecretProvider is configured to '{provider}' for server '{_server.Id}', but no secret retriever is registered.");
            }

            string path = !string.IsNullOrWhiteSpace(_server.SecretPath) ? _server.SecretPath : _server.Url;
            string field = !string.IsNullOrWhiteSpace(_server.SecretField)
                ? _server.SecretField
                : (!string.IsNullOrWhiteSpace(_server.SecretItemKey) ? _server.SecretItemKey : "ApiKey");

            if (!string.IsNullOrWhiteSpace(_server.SecretMount))
            {
                path = $"{_server.SecretMount}:{path}";
            }
            else if (provider.Equals("Vault", StringComparison.OrdinalIgnoreCase) &&
                     string.IsNullOrWhiteSpace(_server.SecretPath) &&
                     !string.IsNullOrWhiteSpace(_server.SecretItemKey))
            {
                var parts = _server.SecretItemKey.Split(':', 3);
                if (parts.Length == 3)
                {
                    path = $"{parts[0]}:{parts[1]}";
                    field = parts[2];
                }
            }

            string? secret = null;
            if (retriever is CompositeSecretRetriever composite)
            {
                secret = await composite.GetSecretForProviderAsync(provider, path, field);
            }
            else
            {
                secret = await retriever.GetSecretAsync(path, field);
            }

            if (string.IsNullOrEmpty(secret))
            {
                throw new System.Security.SecurityException($"Failed to resolve secret from provider '{provider}' for server '{_server.Id}' (path: '{path}', field: '{field}'). Plaintext ApiKey fallback is disabled.");
            }

            return secret;
        }

        private string ExpandVariables(string input, string? resolvedToken, string? secretItemKey)
        {
            if (string.IsNullOrEmpty(input)) return input;

            if (!string.IsNullOrEmpty(secretItemKey) && !string.IsNullOrEmpty(resolvedToken))
            {
                input = input.Replace($"%{secretItemKey}%", resolvedToken, StringComparison.OrdinalIgnoreCase);
                input = input.Replace($"${secretItemKey}", resolvedToken, StringComparison.OrdinalIgnoreCase);
            }

            foreach (System.Collections.DictionaryEntry de in Environment.GetEnvironmentVariables())
            {
                var key = de.Key?.ToString();
                var val = de.Value?.ToString();
                if (!string.IsNullOrEmpty(key) && val != null)
                {
                    input = input.Replace($"%{key}%", val, StringComparison.OrdinalIgnoreCase);
                    input = input.Replace($"${key}", val, StringComparison.OrdinalIgnoreCase);
                }
            }

            return input;
        }

        public static List<string> ParseCommandLine(string commandLine)
        {
            var args = new List<string>();
            if (string.IsNullOrWhiteSpace(commandLine)) return args;

            var current = new StringBuilder();
            bool inQuotes = false;
            char quoteChar = '\0';

            for (int i = 0; i < commandLine.Length; i++)
            {
                char c = commandLine[i];
                if (inQuotes)
                {
                    if (c == quoteChar)
                    {
                        inQuotes = false;
                        quoteChar = '\0';
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else
                {
                    if (c == '"' || c == '\'')
                    {
                        inQuotes = true;
                        quoteChar = c;
                    }
                    else if (char.IsWhiteSpace(c))
                    {
                        if (current.Length > 0)
                        {
                            args.Add(current.ToString());
                            current.Clear();
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
            }
            if (current.Length > 0)
            {
                args.Add(current.ToString());
            }
            return args;
        }

        private void ValidateSecurityPolicy(string executable)
        {
            if (string.IsNullOrWhiteSpace(executable))
            {
                throw new System.Security.SecurityException("Command/executable is empty or invalid.");
            }

            char[] unsafeChars = { ';', '&', '|', '<', '>', '\n', '\r', '`', '$', '*' };
            if (executable.Any(c => unsafeChars.Contains(c)))
            {
                throw new System.Security.SecurityException($"Command '{executable}' contains disallowed unsafe characters.");
            }

            var lowerExec = Path.GetFileNameWithoutExtension(executable).ToLowerInvariant();
            string[] blockedExecutables = { "sh", "bash", "cmd", "powershell", "pwsh", "zsh" };
            if (blockedExecutables.Contains(lowerExec))
            {
                throw new System.Security.SecurityException($"Direct invocation of shell '{executable}' is blocked under the security policy.");
            }
        }

        public async Task ConnectAsync()
        {
            string? token = null;
            try
            {
                token = await ResolveTokenAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve secret token for STDIO backend {ServerId}", _server.Id);
            }

            var expandedCommandLine = ExpandVariables(_server.Url, token, _server.SecretItemKey);

            var parsed = ParseCommandLine(expandedCommandLine);
            if (parsed.Count == 0)
            {
                throw new InvalidOperationException("STDIO backend command line is empty.");
            }

            var executable = parsed[0];
            ValidateSecurityPolicy(executable);

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = AppContext.BaseDirectory
            };

            for (int i = 1; i < parsed.Count; i++)
            {
                startInfo.ArgumentList.Add(parsed[i]);
            }

            if (!string.IsNullOrEmpty(_server.SecretItemKey) && !string.IsNullOrEmpty(token))
            {
                startInfo.Environment[_server.SecretItemKey] = token;
            }

            _logger.LogInformation("Launching STDIO backend process {ServerId}: {Executable}...", _server.Id, executable);

            _process = new System.Diagnostics.Process { StartInfo = startInfo };

            try
            {
                if (!_process.Start())
                {
                    throw new Exception("Process failed to start.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start STDIO backend process {ServerId} (Command: {Command})", _server.Id, executable);
                throw new InvalidOperationException($"Failed to launch STDIO backend process: {ex.Message}", ex);
            }

            if (_process.HasExited)
            {
                throw new InvalidOperationException($"STDIO backend process exited immediately with code {_process.ExitCode}");
            }
        }

        public void StartReader(Func<JsonRpcMessage, Task> onMessageReceived)
        {
            _readerTask = Task.Run(async () =>
            {
                try
                {
                    while (!_cts.Token.IsCancellationRequested && _process != null && !_process.HasExited)
                    {
                        string? line = await _process.StandardOutput.ReadLineAsync(_cts.Token);
                        if (line == null)
                        {
                            break; // EOF
                        }

                        if (string.IsNullOrWhiteSpace(line)) continue;

                        try
                        {
                            var message = JsonSerializer.Deserialize<JsonRpcMessage>(line, _jsonOptions);
                            if (message != null)
                            {
                                if (message is JsonRpcResponse response && response.Id != null)
                                {
                                    var idStr = response.Id.ToString();
                                    if (idStr != null)
                                    {
                                        _stateManager.TryCompleteRequest(idStr, response);
                                    }
                                }

                                if (message is not JsonRpcResponse)
                                {
                                    _logger.LogDebug("[JSON-RPC Backend {ServerId} -> Gateway] {Payload}", _server.Id, McpRouter.Core.Logging.PiiSanitizer.SanitizePayload(line));
                                }
                                await onMessageReceived(message);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to parse STDIO message data: {Data}", line);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Clean cancellation
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading from STDIO backend {ServerId} stdout", _server.Id);
                }
                finally
                {
                    HandleProcessExit();
                }
            });

            _stderrTask = Task.Run(async () =>
            {
                try
                {
                    while (!_cts.Token.IsCancellationRequested && _process != null && !_process.HasExited)
                    {
                        string? line = await _process.StandardError.ReadLineAsync(_cts.Token);
                        if (line == null)
                        {
                            break; // EOF
                        }

                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            _logger.LogWarning("[STDIO Backend {ServerId} Stderr] {Message}", _server.Id, McpRouter.Core.Logging.PiiSanitizer.SanitizePayload(line));
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Clean cancellation
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading from STDIO backend {ServerId} stderr", _server.Id);
                }
            });
        }

        public async Task<JsonRpcResponse> SendRequestAsync(string method, string bodyJson)
        {
            if (_process == null || _process.HasExited)
            {
                return new JsonRpcResponse { Error = new JsonRpcError { Code = -32001, Message = "Process not running" } };
            }

            string requestId = Guid.NewGuid().ToString("N");
            string modifiedBody = bodyJson;

            try
            {
                using var doc = JsonDocument.Parse(bodyJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("id", out var idProp))
                {
                    requestId = idProp.ToString();
                }
                else
                {
                    var dict = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(bodyJson) ?? new();
                    dict["id"] = requestId;
                    modifiedBody = JsonSerializer.Serialize(dict);
                }
            } catch { }

            var tcs = _stateManager.CreateRequest(requestId);

            try
            {
                _logger.LogDebug("[JSON-RPC Gateway -> Backend {ServerId}] {Payload}", _server.Id, McpRouter.Core.Logging.PiiSanitizer.SanitizePayload(modifiedBody));

                await _writeLock.WaitAsync(_cts.Token);
                try
                {
                    await _process.StandardInput.WriteLineAsync(modifiedBody);
                    await _process.StandardInput.FlushAsync();
                }
                finally
                {
                    _writeLock.Release();
                }

                var response = await tcs.Task.WaitAsync(RequestTimeout, _cts.Token);
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                _logger.LogDebug("[JSON-RPC Backend {ServerId} -> Gateway] {Payload}", _server.Id, McpRouter.Core.Logging.PiiSanitizer.SanitizePayload(responseJson));
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending request to STDIO backend {ServerId} (Method: {Method})", _server.Id, method);
                throw;
            }
            finally
            {
                _stateManager.TryCompleteRequest(requestId, null!);
            }
        }

        public async Task<JsonRpcResponse> CallMethodAsync(string method, object parameters, string? overrideId = null)
        {
            var bodyObj = new { jsonrpc = "2.0", method = method, @params = parameters, id = overrideId ?? Guid.NewGuid().ToString("N") };
            var bodyJson = JsonSerializer.Serialize(bodyObj);
            return await SendRequestAsync(method, bodyJson);
        }

        public async Task SendNotificationAsync(string method, string bodyJson)
        {
            if (_process == null || _process.HasExited) return;

            _logger.LogDebug("[JSON-RPC Gateway -> Backend {ServerId}] [Notification] {Payload}", _server.Id, McpRouter.Core.Logging.PiiSanitizer.SanitizePayload(bodyJson));

            await _writeLock.WaitAsync(_cts.Token);
            try
            {
                await _process.StandardInput.WriteLineAsync(bodyJson);
                await _process.StandardInput.FlushAsync();
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public async Task SendResponseAsync(string responseJson)
        {
            if (_process == null || _process.HasExited) return;

            _logger.LogDebug("[JSON-RPC Gateway -> Backend {ServerId}] [Response] {Payload}", _server.Id, McpRouter.Core.Logging.PiiSanitizer.SanitizePayload(responseJson));

            await _writeLock.WaitAsync(_cts.Token);
            try
            {
                await _process.StandardInput.WriteLineAsync(responseJson);
                await _process.StandardInput.FlushAsync();
            }
            finally
            {
                _writeLock.Release();
            }
        }

        private void HandleProcessExit()
        {
            if (Interlocked.CompareExchange(ref _exitHandled, 1, 0) == 0)
            {
                _stateManager.CancelAll();

                if (_process != null)
                {
                    try
                    {
                        if (!_process.HasExited)
                        {
                            _logger.LogInformation("Stopping STDIO backend process {ServerId}...", _server.Id);
                            _process.StandardInput.Close();

                            if (!_process.WaitForExit(3000))
                            {
                                _logger.LogWarning("STDIO backend process {ServerId} did not exit gracefully. Killing process tree...", _server.Id);
                                _process.Kill(entireProcessTree: true);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("STDIO backend process {ServerId} exited unexpectedly with code {ExitCode}.", _server.Id, _process.ExitCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error handling STDIO backend process {ServerId} exit", _server.Id);
                    }
                }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            HandleProcessExit();
            _writeLock.Dispose();
            _cts.Dispose();
        }
    }
}
