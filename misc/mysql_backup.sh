#!/bin/bash

# Config
TIMESTAMP=$(date +"%F-%H-%M-%S")
BACKUP_DIR="/opt/db_backups"
MYSQL_USER=""
MYSQL_PASSWORD=""
DATABASE="g2"
BACKUP_FILE="$BACKUP_DIR/${DATABASE}_$TIMESTAMP.sql.gz"
RCLONE_REMOTE="gdrive:db_backups"

# Make sure backup directory exists
mkdir -p "$BACKUP_DIR"

# Dump database
mysqldump -u $MYSQL_USER $DATABASE -p'' | gzip > "$BACKUP_FILE"

# Upload to Google Drive
rclone copy "$BACKUP_FILE" "$RCLONE_REMOTE"

echo "Backup completed at $BACKUP_FILE and uploaded to $RCLONE_REMOTE"

