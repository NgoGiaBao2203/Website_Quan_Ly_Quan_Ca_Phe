@echo off
setlocal

set "imagesDir=WebCoffeeApplication\wwwroot\images\drinks"
if not exist "%imagesDir%" (
  mkdir "%imagesDir%"
)

echo Images folder ready: %imagesDir%
endlocal
