Option Explicit
if WScript.Arguments.Count < 4 then
    WScript.Echo "Missing parameters"
    WScript.Echo "Usage: ReplaceFile.vbs inputFile toSearchText toReplaceText outputFile"
end if

Dim inputFile,outputFile,fileContent,toSearch,toReplace,inputStream,outputStream

inputFile = WScript.Arguments(0)
toSearch = WScript.Arguments(1)
toReplace = WScript.Arguments(2)
outputFile = WScript.Arguments(3)

Set inputStream = CreateObject("ADODB.Stream")
inputStream.CharSet = "utf-8"
inputStream.Open
inputStream.Type     = 2 'text
inputStream.Position = 0
inputStream.LoadFromFile(inputFile)

fileContent = inputStream.ReadText()
fileContent = Replace(fileContent,toSearch,toReplace)

Set outputStream = CreateObject("ADODB.Stream")
outputStream.Open
outputStream.Type     = 2 'text
outputStream.Position = 0
outputStream.Charset  = "utf-8"
outputStream.WriteText fileContent
outputStream.SaveToFile outputFile, 2
outputStream.Close
