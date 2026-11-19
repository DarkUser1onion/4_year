#!/bin/sh
# Запускает пример из его папки. Чем запускать, решает расширение.
#
#   run primery/03.py        один пример
#   run primery              все примеры папки по порядку
#   run primery\03.go        обратные слэши из PowerShell тоже годятся

set -e

if [ $# -eq 0 ]; then
  echo "Укажите файл, например: docker compose run --rm python primery/01.py" >&2
  exit 2
fi

target=$(printf '%s' "$1" | tr '\\' '/' | sed 's#^\./##; s#/$##')
shift

if [ -d "/work/$target" ]; then
  found=0
  for file in "/work/$target"/*.py "/work/$target"/*.rb "/work/$target"/*.go "/work/$target"/*.cpp; do
    [ -f "$file" ] || continue
    found=1
    name=${file#/work/}
    printf '\n── %s ──\n' "$name"
    code=0
    sh /work/docker/run.sh "$name" || code=$?
    [ "$code" -eq 0 ] || printf '(программа завершилась с ошибкой, код %s)\n' "$code"
  done
  [ "$found" -eq 1 ] || { echo "В папке $target нет программ" >&2; exit 2; }
  exit 0
fi

if [ ! -f "/work/$target" ]; then
  echo "Файл не найден: $target (путь считается от папки архива)" >&2
  exit 2
fi

cd "/work/$(dirname "$target")"
file=$(basename "$target")

case "$file" in
  *.py)
    exec python "$file" "$@" ;;
  *.rb)
    exec ruby "$file" "$@" ;;
  *.go)
    exec go run "$file" "$@" ;;
  *.cpp)
    binary="/tmp/${file%.cpp}"
    driver=$(pkg-config --list-all | awk '/^libmongocxx/ { print $1; exit }')
    # shellcheck disable=SC2046
    g++ -std=c++17 "$file" -o "$binary" $(pkg-config --cflags --libs "$driver")
    exec "$binary" "$@" ;;
  *)
    echo "Не знаю, чем запускать $file: нужен .py, .rb, .go или .cpp" >&2
    exit 2 ;;
esac
