awk '
/IF OBJECT_ID\(.dbo.AppKeys., .U.\) IS NULL/ {
    print "                    IF OBJECT_ID('\''dbo.UserServerCredentials'\'', '\''U'\'') IS NULL"
    print "                    BEGIN"
    print "                        CREATE TABLE [dbo].[UserServerCredentials] ("
    print "                            [Id]                  VARCHAR(100) PRIMARY KEY,"
    print "                            [Username]            NVARCHAR(200) NOT NULL,"
    print "                            [ServerId]            VARCHAR(100) NOT NULL,"
    print "                            [EncryptedSecretJson] NVARCHAR(MAX) NULL"
    print "                        );"
    print "                    END;"
    print ""
    print $0
    next
}
{ print }
' Infrastructure/Persistence/DatabaseSeederService.cs > DatabaseSeederService.tmp
mv DatabaseSeederService.tmp Infrastructure/Persistence/DatabaseSeederService.cs
