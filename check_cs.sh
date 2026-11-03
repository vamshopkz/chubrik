#!/bin/zsh
# Быстрая проверка, что C# компилируется (без запуска Unity): компилятор Roslyn из Unity + ссылки из последней сборки.
HERE=~/CODE/chubrik/UnityProject
U=/Applications/Unity/Hub/Editor/6000.3.21f1/Unity.app/Contents/Resources/Scripting
RSP=$(ls -t $HERE/Library/Bee/artifacts/*P.dag/Assembly-CSharp.rsp | head -1)
OUT=${TMPDIR:-/tmp}/chubrik_check; mkdir -p $OUT
grep -vE '^"Assets/|^-out:|^-refout:|^-analyzer:' $RSP > $OUT/args.rsp
echo "-out:\"$OUT/check.dll\"" >> $OUT/args.rsp
cd $HERE
find Assets/Scripts -name "*.cs" -not -path "*/Editor/*" | sed 's/.*/"&"/' >> $OUT/args.rsp
nice -n 10 $U/NetCoreRuntime/dotnet $U/DotNetSdkRoslyn/csc.dll @$OUT/args.rsp 2>&1 | grep -E "error|warning CS0162|Compilation" | sort -u | head -40
echo "check done"
