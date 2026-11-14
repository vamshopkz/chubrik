#!/bin/zsh
# Браузерная версия на GitHub Pages (после BuildScript.BuildWebGL → UnityProject/Builds/WebGL).
set -e
HERE=~/CODE/chubrik
GH=~/CODE/hay-hunter/tools/gh/bin/gh
SRC=$HERE/UnityProject/Builds/WebGL
[ -f $SRC/index.html ] || { echo "Нет сборки $SRC"; exit 1; }
LOGIN=$($GH api user -q .login)
REPO="$LOGIN/chubrik"
DIR=$HERE/web-publish
rm -rf $DIR; mkdir -p $DIR
VER=$(grep -m1 "bundleVersion:" $HERE/UnityProject/ProjectSettings/ProjectSettings.asset | awk '{print $2}')
# keep every earlier version that is on the site (v/VER/) — «Версии игры» in the settings opens them
OLD=$HERE/web-old; rm -rf $OLD
git -c credential.helper= -c credential.helper="!$GH auth git-credential" clone -q --depth 1 -b gh-pages "https://github.com/$REPO.git" $OLD 2>/dev/null || true
[ -d $OLD/v ] && cp -R $OLD/v $DIR/
# the version that was on the front page until now also moves into its own folder
OV=$(python3 -c "import json,sys; print(json.load(open(sys.argv[1]))['version'])" $OLD/version.json 2>/dev/null || true)
if [ -n "$OV" ] && [ "$OV" != "$VER" ] && [ ! -d $DIR/v/$OV ]; then
  mkdir -p $DIR/v/$OV
  for f in index.html version.json icon.png apple-touch-icon.png Build TemplateData StreamingAssets; do [ -e $OLD/$f ] && cp -R $OLD/$f $DIR/v/$OV/; done
fi
rm -rf $OLD
cp -R $SRC/ $DIR/
touch $DIR/.nojekyll
# the game in the browser compares this with its own version and shows «ОБНОВИТЬ» when the page is newer
printf '{"version": "%s"}' "$VER" > $DIR/version.json
# this version also gets its own folder, so it can still be played after the next release
mkdir -p $DIR/v/$VER
( cd $SRC && cp -R . $DIR/v/$VER/ ); cp $DIR/version.json $DIR/v/$VER/
cd $DIR
git init -q -b gh-pages
git add -A
git -c user.name="$LOGIN" -c user.email="$LOGIN@users.noreply.github.com" commit -q \
  -m "Браузерная версия $(date +%Y-%m-%d\ %H:%M)" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
git -c http.postBuffer=524288000 -c credential.helper= -c credential.helper="!$GH auth git-credential" push -q -f "https://github.com/$REPO.git" gh-pages
$GH api "repos/$REPO/pages" >/dev/null 2>&1 || $GH api -X POST "repos/$REPO/pages" -f "source[branch]=gh-pages" -f "source[path]=/" >/dev/null
echo "Готово: https://$LOGIN.github.io/chubrik/"
