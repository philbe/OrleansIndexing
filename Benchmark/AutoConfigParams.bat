@ECHO off

REM ===========================================================
REM NECESSARY PARAMETERS SECTION
REM ===========================================================

REM service configuration name
SET "CLOUDFILENAME=CloudEuropeWest"

REM cloud service name
SET "DOMAINCLOUDAPP=mdashtiepflcloud"

REM conductor cloud app name
SET "CONDUCTORDOMAINCLOUDAPP=mdashtiepflcloudconductor"

REM main storage account
SET "MAINSTORAGEACCOUNT=mdashtiepflstorage"

REM main storage account key
SET "MAINSTORAGEACCOUNTKEY=YourMainStorageAccountKey"

REM grain storage account
SET "GRAINSTORAGEACCOUNT=mdashtiepflstoragegrain"

REM grain storage account key
SET "GRAINSTORAGEACCOUNTKEY=YourGrainStorageAccountKey"

REM indexing storage account
SET "INDEXINGSTORAGEACCOUNT=mdashti82d1988sggrain"

REM indexing storage account key
SET "INDEXINGSTORAGEACCOUNTKEY=6TIHaTRQCboBB7it3Yv5eUZyFf+HOF0gfFnaABkQm9ApXdccVucGO9Bd3vOGBxlvCgnoMV+UEZW5+H6zeVBiWg=="

REM indexing workflow storage account
SET "INDEXINGWORKFLOWSTORAGEACCOUNT=mdashti82d1988sggrain"

REM indexing workflow storage account key
SET "INDEXINGWORKFLOWSTORAGEACCOUNTKEY=6TIHaTRQCboBB7it3Yv5eUZyFf+HOF0gfFnaABkQm9ApXdccVucGO9Bd3vOGBxlvCgnoMV+UEZW5+H6zeVBiWg=="

REM DocumentDB endpoint URL
SET "DOCUMENTDBURL=https://mdashtiepfldocumentdb.documents.azure.com:443/"

REM DocumentDB Key
SET "DOCUMENTDBKEY=YourDocDBKey"

REM DocumentDB Offer Type Version (V1 or V2)
REM refer to this link for more info: https://docs.microsoft.com/en-us/azure/documentdb/documentdb-performance-levels
REM SET "DOCUMENTDBOFFERTYPEVERSION=V1"

REM DocumentDB Offer Type
REM If Offer Type is V1, then the possible values are S1, S2 or S3
REM Otherwise, if Offer Type is V2, then RU (request units per second) as integer
REM refer to this link for more info: https://docs.microsoft.com/en-us/azure/documentdb/documentdb-performance-levels
SET "DOCUMENTDBOFFERTYPE=240100"

REM DocumentDB Indexing Mode. The possible opetions are "consistent" and "lazy".
SET "DOCUMENTDBINDEXINGMODE=consistent"

REM ===========================================================
REM ONLY CHANGE THESE PARAMETERS IF YOU KNOW THEIR VALUE,
REM OTHERWISE YOU'LL CHANGE THEM LATER MANUALLY IN THE PROJECT
REM ===========================================================

REM windows-azure plugin thumbprint for the conductor
SET "WINDOWSAZUREPLUGINTUMBPRINTCONDUCTOR=6E3303FBEE710F8DA9C4794642BECA756188B930"

REM remote desktop use for the conductor
SET "REMOTEDESKTOPUSERCONDUCTOR=dashti"

REM remote desktop password for the conductor
SET "REMOTEDESKTOPPASSCONDUCTOR=MIIBnQYJKoZIhvcNAQcDoIIBjjCCAYoCAQAxggFOMIIBSgIBADAyMB4xHDAaBgNVBAMME1dpbmRvd3MgQXp1cmUgVG9vbHMCEHJ9qsB0cVCwQKlTDdoYLgswDQYJKoZIhvcNAQEBBQAEggEAZzSBy8WwKSyI/25uzVqAssymVvenJd54Jq7c9wYZqrSLHPUtwnEwBqnFd5KyknPewX0Cs1RcqUTWlWWloY0WBc2/qhqdy4v5+CMo8pBJlWKvPn5kDdhybn/YlOp2Ppl3e7SMiCXdSCfP282m1/03MXtDZoL330RQp3oSVaQrJle2lLJh/m89c2c4EqJoYXLjntz27I8fflEJFm+WN1APfsg+vsNDutXFpjvrTmKl/dR7eRVH/MPjGXzMgEjTHrzb+44lMtu7boVEGJ9iWJDLn2BlvX4KaKhnPhJTXfD4ePQMd6y0IeOs/2oQHHyDntUAtHH7P25gjnmGX5ZzorjfYzAzBgkqhkiG9w0BBwEwFAYIKoZIhvcNAwcECA1Ca0bw+/ypgBCjS3ex3+4ojwz0QaPvlLmB"

REM windows-azure plugin thumbprint for Orleans
SET "WINDOWSAZUREPLUGINTUMBPRINT=E3D564F5ED98A62F66CB7C9B4842B3DD3A3D8CAE"

REM remote desktop user for Orleans
SET "REMOTEDESKTOPUSER=dashti"

REM remote desktop password for Orleans
SET "REMOTEDESKTOPPASS=MIIBnQYJKoZIhvcNAQcDoIIBjjCCAYoCAQAxggFOMIIBSgIBADAyMB4xHDAaBgNVBAMME1dpbmRvd3MgQXp1cmUgVG9vbHMCECGJ2fnOORWeRHyqYpxXbBAwDQYJKoZIhvcNAQEBBQAEggEAgUtbp+Q089a5lFXm66KoNbLL93Mm3En7aFglCWjNPNT92fYWRM7nohW59K4Y1XJHOBW5+UeZY2cLNPk0vTVhRQgQBOiIJWLxA7ug6t9fQR+HVpIg65Fc4OSp9AFCWBoDemZA6Tho/l3FHFL+v50PalumHrSuBfCgs1/h+8Ox72+dUtNQjNejZQ7eiAdd9VEsxBF2YEBpMFiKHAjL9gcyDFoKNnoeIYHTacGh7jOBm5iIk92AKO5C+Z+LHhkT0gTqPkEjASS9Xeue4FBfgTltCqIS6Wdsw1O1H4RFtxbkUU5Qg3fIChzfV4WOI3A1/61V1gyzioXY658XVptpQabhhjAzBgkqhkiG9w0BBwEwFAYIKoZIhvcNAwcECDRbf7/t9tE6gBB4DxTvzI1p3rjRuABto2TM"
