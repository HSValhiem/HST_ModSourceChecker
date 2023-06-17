## Info
Drag a Mod onto the Tool to check if it uses sources that have been changed or deleted in the game update.
The tool will provide the name of the classes that are changed or deleted for you to reference within WinMerge for changes.

## Instructions to Use
### Prerequisite Download Required Tools
[Notepad++](https://notepad-plus-plus.org/downloads/v8.5.3)
[DNSpy](https://github.com/dnSpyEx/dnSpy/releases/tag/v6.3.0)
[WinMerge](https://winmerge.org/?lang=en)

**Note: You will need both the last version of the game and the current version to compare against.
If you don't have the old version I.E Steam has already updated automatically then follow the guide at the bottom of this page to get it.**

### Step 1: Extract Game Sources with DNSpy
**Note: You will need to repeat this step for both Versions of the game(Old and New) and use different directories**
**Remember where you save both projects as the location will need to be used later**
- Open DNSpy
- Click "File->Open" in Toolbar at top
- Open assembly_valheim.dll in "Valheim\valheim_Data\Managed"
![image](https://github.com/HSValhiem/modSourceChecker/assets/18600015/fadcf0fa-bfb7-421e-bc6b-5711bbbada36)
- Click "File->Export to Project"  in Toolbar at top
- Select a Folder to save to and press Export
**Note: Use the Picture Below for Reference, Don't forget to set Visual Studio version to 2022**
![image](https://github.com/HSValhiem/modSourceChecker/assets/18600015/eaf38f75-63e0-4bf2-90d9-abbfa7a7ef66)
**Don't forget to extract both versions of the game!**

### Step 2: Use Notepad++ to Remove Token Tags with Regex
**Note: You will need to repeat this step for both Versions of the game(Old and New)**
- Open Notepad++
- Click "Search->Find in Files..." in Toolbar at top
- Enter the following `^\s*\/\/(?:\s*\((get|add|invoke|set|remove)\))?\s*Token:.+$` into the "Find what:" box
- Browse or Enter the Path to the folder that you exported the source to with DNSpy
- Make sure the search is setup like the following image and press "Replace in Files" Button and Press OK to Confirm 
![image](https://github.com/HSValhiem/modSourceChecker/assets/18600015/02dc474c-6eb7-4fc0-8ee9-566b6fa8b4a3)
**Don't forget to do this for both versions of the game!(Both Directories Old and New)**

### Step 3: Use WinMerge to Check Differences
- Open WinMerge
- Click "File->Open" in Toolbar at top
- Open the old version dump directory and new version dump directory in Minmerge
**Note: Make sure to select the directory that contains the .cs files not the .sln**
- Click the Options Button and Make sure that the Options Look Like the Second Picture
- Make sure Everything looks like the Picture and Click the Compare Button
**Note: These are the Locations that you saved During the DNSpy Step**
**Note: Make sure that the First Folder is the Old Valheim Version and that the Second Folder is the New Version**
**Note: Click the Options Button and Make sure that the Options Look Like the Second Picture**
![image](https://github.com/HSValhiem/modSourceChecker/assets/18600015/542e0a33-6e66-42f2-bc85-70a9884c8983)
![image](https://github.com/HSValhiem/modSourceChecker/assets/18600015/278edcbc-f4d1-4cc3-a171-2c10749db6c6)

- Click "Tools->Generate Report" in Toolbar at top
- Save the file with name "newVersion" to the same directory that modSourceChecker.exe is located and select CSV for the "Style"
![image](https://github.com/HSValhiem/modSourceChecker/assets/18600015/e9aa9392-7cf2-43fe-97ea-89c5b7ac1713)
![image](https://github.com/HSValhiem/modSourceChecker/assets/18600015/89245055-2cc4-472e-b088-0cb377859dad)

### Step 4: Use It!!!
- Simply Drag the Mod onto the modSourceChecker.exe and it will tell you if the Mod has source code references that have been changed or deleted in between version updates for Valheim.
- One you have the classname in question you can go back into WinMerge and see the changes.
![image](https://github.com/HSValhiem/modSourceChecker/assets/18600015/dccce552-7b86-407e-b63e-58956ad2925e)
![image](https://github.com/HSValhiem/modSourceChecker/assets/18600015/1d46cdb3-c1e9-4365-9196-69181fa5be5f)
![image](https://github.com/HSValhiem/modSourceChecker/assets/18600015/a44a996b-ebde-4c74-a8ae-462c976194b9)

# How to get last version of Valheim in steam Tutorial
- Open Steam
- Right Click on Valheim and Select Properties
![image](https://github.com/HSValhiem/modSourceChecker/assets/18600015/5cb21b5c-1c6f-47ed-a9c6-171994033d9a)
- Select "Betas" on the Left Side and then in the Dropdown select default_old
![image](https://github.com/HSValhiem/modSourceChecker/assets/18600015/870fa26b-ae4b-451e-bc41-7f804100272f)



