# Changed the title from OpenWolfPack to WiseOwlChat.

If your local Git repository's remote URL is outdated, you probably need to update it.
```
git remote set-url origin https://github.com/katsumiar/WiseOwlChat.git
```

# WiseOwlChat
![sample](https://github.com/katsumiar/WiseOwlChat/assets/63950487/d10819fd-875a-4146-88f8-45def8065af2)

## Features
WiseOwlChat has the following features.
- Natural language understanding function that understands user intent and supports communication
- Content enrichment feature that automatically adds supplementary information to user requests
- Ability to call custom functions via plugins
- File and URL processing functions that support file uploads and URL references
- Seamless integration with your website
- Prohibited term management function to manage expressions outside the applicable scope
- Pipeline functionality to create and execute coordinated tasks for specific purposes

WiseOwlChat is a chatbot that operates using OpenAI API.

## Supported .NET version:
7.0(Desktop)

## Getting Started
Set the directory containing the `WiseOwlChat.sln` file as your current directory, and execute the `dotnet build` command.  
The executable file (WiseOwlChat.exe) will be created in the following directory:  
`WiseOwlChat\bin\Debug\net7.0-windows`

```
dotnet build
```

Please copy the DLL (.dll) files located under the directories where the plugin files are generated to the following directory:  
`WiseOwlChat\bin\Debug\net7.0-windows\Plugins`  
  
**Issue with third-party package DLL not being copied has been resolved.**

### Directories where Plugin Files are Generated
`FetchUrlAPI\bin\Debug\net7.0`  
`HolidayCalendarAPI\bin\Debug\net7.0`  
`PythonInterpreterAPI\bin\Debug\net7.0`  
`ReadFileAPI\bin\Debug\net7.0`  
**When you build WiseOwlChat.sln, it will be automatically copied.**  

## API Key Requirement
To use OpenAI API, you need to obtain an OpenAI API key in advance.  
Register the obtained API key as an environment variable with the name `OPENAI_AI_KEY` or `OPENAI_KEY` (Note: The purpose is the same, just the name is different).  

## Support
If you have any questions, please contact WiseOwlChat.  
  
<img src="https://github.com/katsumiar/WiseOwlChat/assets/63950487/39264774-b53d-436d-ad05-f0d5bf9904cb" width="50%">  
  
[For more details, please read the wiki.](https://github.com/katsumiar/WiseOwlChat/wiki)  

## License
MIT License (MIT)  
