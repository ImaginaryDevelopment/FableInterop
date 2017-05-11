rem http://stackoverflow.com/questions/34922908/how-to-run-multiple-commands-in-a-batch-file
call npm install
echo "next"
call dotnet restore
echo "restored"
rem http://stackoverflow.com/questions/303838/create-a-new-cmd-exe-window-from-within-another-cmd-exe-prompt
call start cmd.exe @cmd /k dotnet fable start
timeout 3
npm run start