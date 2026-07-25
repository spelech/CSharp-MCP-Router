export function escapeHtml(str) {
    if (!str) return '';
    return str.replace(/&/g, "&amp;")
              .replace(/</g, "&lt;")
              .replace(/>/g, "&gt;")
              .replace(/"/g, "&quot;")
              .replace(/'/g, "&#039;");
}

export function showToast(message, type = 'info', duration = 5000) {
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    toast.className = `toast-card toast-${type}`;

    let iconClass = 'fa-circle-info';
    if (type === 'success') iconClass = 'fa-circle-check';
    if (type === 'error') iconClass = 'fa-circle-exclamation';

    toast.innerHTML = `
        <div class="toast-content">
            <i class="fa-solid ${iconClass}"></i>
            <span>${escapeHtml(message)}</span>
        </div>
        <button type="button" class="toast-close"><i class="fa-solid fa-xmark"></i></button>
    `;

    container.appendChild(toast);

    // Auto dismiss
    const dismissTimeout = setTimeout(() => {
        dismissToast(toast);
    }, duration);

    // Close button click handler
    toast.querySelector('.toast-close').addEventListener('click', () => {
        clearTimeout(dismissTimeout);
        dismissToast(toast);
    });
}

function dismissToast(toast) {
    toast.classList.add('toast-fade-out');
    toast.addEventListener('animationend', (e) => {
        if (e.animationName === 'toast-out') {
            toast.remove();
        }
    });
}

