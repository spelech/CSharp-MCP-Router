sed -i 's/GlobalMaxKeys INTEGER DEFAULT 100,/GlobalMaxKeys INTEGER DEFAULT 100,\n                        UserMaxKeys INTEGER DEFAULT 5,\n                        UserSecretStorage TEXT DEFAULT '"'Database'"'/g' Infrastructure/Persistence/DatabaseSeederService.cs

sed -i 's/UserMaxKeys INTEGER DEFAULT 5/UserMaxKeys INTEGER DEFAULT 5,\n                        UserSecretStorage TEXT DEFAULT '"'Database'"'/g' Infrastructure/Persistence/DatabaseSeederService.cs

sed -i 's/\[UserMaxKeys\]             INT NOT NULL DEFAULT 5/\[UserMaxKeys\]             INT NOT NULL DEFAULT 5,\n                            \[UserSecretStorage\]       VARCHAR(50) NOT NULL DEFAULT '"'Database'"'/g' Infrastructure/Persistence/DatabaseSeederService.cs

sed -i 's/`UserMaxKeys`             INT NOT NULL DEFAULT 5/`UserMaxKeys`             INT NOT NULL DEFAULT 5,\n                        `UserSecretStorage`       VARCHAR(50) NOT NULL DEFAULT '"'Database'"'/g' Infrastructure/Persistence/DatabaseSeederService.cs

