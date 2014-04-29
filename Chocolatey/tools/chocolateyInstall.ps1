$packageName = 'Hasher'
$url = 'http://arcanesanctum.net/wp-content/uploads/hasher/last.zip'

Install-ChocolateyZipPackage "$packageName" "$url" "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"