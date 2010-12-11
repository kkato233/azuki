@echo off
setlocal

pushd doc
  msbuild  /p:Configuration=Release  Document.shfbproj
popd
