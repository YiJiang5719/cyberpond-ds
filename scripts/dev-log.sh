#!/bin/bash
# CyberPond 开发日志自动生成脚本
# 用法：bash scripts/dev-log.sh

PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
LOG_DIR="$PROJECT_DIR/dev-logs"
DATE=$(date +%Y-%m-%d)
LOG_FILE="$LOG_DIR/$DATE.md"

if [ -f "$LOG_FILE" ]; then
  echo "[dev-log] 今日日志已存在: $LOG_FILE"
  exit 0
fi

cat > "$LOG_FILE" <<EOF
# 开发日志 — $DATE

## 完成事项
-

## 待办事项
-

## 遇到的问题
-
EOF

echo "[dev-log] 已创建日志文件: $LOG_FILE"
