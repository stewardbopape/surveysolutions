echo Build WB.UI.Headquarters\Dependencies
pushd src\UI\Headquarters\WB.UI.Headquarters\Dependencies
call yarn 
call node_modules\.bin\gulp
popd

echo Build WB.UI.Headquarters\HqApp
pushd src\UI\Headquarters\WB.UI.Headquarters\HqApp
call yarn && call yarn run production
popd