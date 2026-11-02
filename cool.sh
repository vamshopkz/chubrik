#!/bin/zsh
# Ждёт, пока Mac не остынет: macOS сообщает о перегреве через pmset (warning level / CPU_Speed_Limit < 100).
for i in {1..40}; do
  T=$(pmset -g therm 2>/dev/null)
  if echo "$T" | grep -qE "CPU_Speed_Limit[[:space:]]*=[[:space:]]*([0-9]|[1-8][0-9])$" || echo "$T" | grep -qiE "warning level[^0-9]*[1-9]"; then
    echo "Mac нагрелся — пауза 30 с ($i)"; sleep 30
  else
    exit 0
  fi
done
