# WiseOwlChat

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

## How to Build
Set the directory containing the `WiseOwlChat.sln` file as your current directory, and execute the `dotnet build` command.
The executable file (WiseOwlChat.exe) will be created in the following directory:  
`WiseOwlChat\bin\Debug\net7.0-windows`

Please copy the DLL (.dll) files located under the directories where the plugin files are generated to the following directory:  
`WiseOwlChat\bin\Debug\net7.0-windows\Plugins`

### Directories where Plugin Files are Generated
`FetchUrlAPI\bin\Debug\net7.0`  
`HolidayCalendarAPI\bin\Debug\net7.0`  
`PythonInterpreterAPI\bin\Debug\net7.0`  
`ReadFileAPI\bin\Debug\net7.0`  

## API Key Requirement
To use OpenAI API, you need to obtain an OpenAI API key in advance.
Register the obtained API key as an environment variable with the name OPENAI_AI_KEY or OPENAI_KEY (Note: The purpose is the same, just the name is different).

## Support
If you have any questions, please contact WiseOwlChat.

## Options
`UI Analyzer (User Intent Analyzer)`: Supports you in understanding user intent.  
`Advice`: Automatically adds supplementary information to user's written content to support them.  
`ReAct`: This function clarifies thoughts, actions, and observations and promotes more structured communication.  
`Training`: AI evolves (This is highly experimental).  
`Pipeline`: Act according to specified procedures.  
`Function`: Enable all checked APIs.  

## Plugins
To implement the `function calling` feature, create a class library containing the methods listed below and place the resulting dll file in the Plugins directory.
It will be automatically loaded and callable.
```
- string FunctionName { get; }
- string Description { get; }
- object Function { get; }
- Task<string> ExecAsync(Action<string> addContent, string param);
```
*Sample source codes for ReadFileAPI.cs and FetchUrlAPI.cs plugins are included.*

## Local File Referencing is Supported
When you drag and drop a file into the input field, the file is placed on a localhost server managed by WiseOwlChat and is replaced with a URL. You can then access it using the `FetchUrlAPI` plugin. If you want to directly refer to the file path, you can use the `ReadFileAPI` plugin.

## URL Referencing is Supported
The bot can respond to URL content, including HTML, PDF, and text files.  
This feature is implemented as a Plugin API (FetchUrlAPI) with multiple specialized methods:
- `FetchWebPageFromUrlAsync`: For fetching HTML content.
- `FetchPdfFromUrlAsync`: For fetching PDF content and converting it to text.
- `FetchTxtFromUrlAsync`: For fetching plain text content.

## Supports file upload.
WiseOwlChat launches a dedicated website as localhost when it starts.
Bots can upload files to this site's dedicated storage.
Users can also download files from this site.
This feature is implemented as a Plugin API (UploadAPI).

## Calendar supported
WiseOwlChat understands today's date, but it doesn't necessarily remember dates, days of the week, and holidays completely.
WiseOwlChat can retrieve a calendar for the desired period with holiday information.
This feature is implemented as a plugin API (HolidayCalendarAPI).

## Python script execution is supported.
Please set the path to Python's DLL in the environment variable `PYTHONNET_PYDLL` in advance.  
Make sure to pre-install useful modules like `sympy`.  
This feature provides LLM with reliable computational capabilities.  
This feature is implemented as a Plugin API (PythonInterpreterAPI).  
**This feature is recommended to be run within a sandbox.**

## Works with the website.
WiseOwlChat launches a dedicated website as localhost at startup.  
When you send a query from a web browser as shown below, WiseOwlChat reads the specified <URL> and responds to the user.  
`http://localhost:55553/queryUrl?url=<URL>`

## Works with the website.
WiseOwlChat launches a dedicated website as localhost at startup.  
- When you send a query from a web browser as shown below, WiseOwlChat will read the specified <URL> and respond to the user.  
`http://localhost:55553/queryUrl?url=<URL>`
- When you send a query from a web browser as shown below, WiseOwlChat will respond to the specified <request content>.  
`http://localhost:55553/query?request=<request content>`

## Forbidden Expressions
If you want to specify forbidden expressions, write them in the forbiddenExpressions.json file.
Title it with the heading of the forbidden expression and specify the corresponding pattern with regex.
If a message sent to the AI matches a forbidden pattern, it won't be sent; instead, a warning will be issued by the AI.

## Pipeline Creation Guide
A pipeline outlines the procedures for executing a series of tasks in a coordinated manner.

### File Formats and Their Roles
- `.pip` (e.g., `create_novel.pip`): Manages the main process flow and overall steps.
  - It will be incorporated into the UI and can be selected as an executable pipeline target.
- `.ida` (e.g., `Chapter_set.ida`, `Character_Design.ida`, etc.): Describes the details and constraints of each step.

### Specific Notations in `.pip` Files (`create_novel.pip`)
- Starting with `:`: This is treated as a comment.
- Starting with `-`: Treats the corresponding `.ida` file as a single prompt.
  - The result can be imported with `{fetch: FileName}`.
- Starting with `@`: Treats the corresponding `.ida` file as special instructions.

### Basic Structure of `.ida` Files Called with `-` (e.g., `Character_Design.ida`)
It is treated as a single prompt.

### Basic Structure of `.ida` Files Called with `@` (e.g., `Chapter_set.ida`)
- Usable Tags and Placeholders
  - `{fetch: FileName}`: Replaced by the result of the specified `.ida` prompt.
  - `{Previous}`: Replaced by the result of the previous section.
  - `{Language}`: Replaced by the language setting.
  - `{now}`: Replaced by the current date and time.
- Dedicated Metadata
  - `$Constraints`: Specifies the constraints.
  - `$Instruction`: Provides specific instructions.
  - `$Each Request Format`: Specifies the format of each request.
  - `$end`: Indicates the end of each of the above.


Copyright: 2023, Katsumi Aradono  
The MIT License (MIT)  



## Third-Party Licenses
This application utilizes the following third-party software.

### For the main application

#### markdig
Copyright: 2018-2019, Alexandre Mutel  
License: BSD 2-Clause "Simplified" License  
This software is provided under the BSD 2-Clause "Simplified" License and is distributed "AS IS." For more details, please refer to the [license file](https://github.com/xoofx/markdig/blob/master/license.txt).

#### Markdig.Wpf
Copyright: 2016-2021, Nicolas Musset  
License: The MIT License (MIT)  
This software is provided "AS IS," under the terms of the MIT License. For more details, please refer to the [license file](https://github.com/Kryptos-FR/markdig.wpf/blob/develop/LICENSE.md).

#### Coltr.Markdig.Wpf.ColorCode
Copyright: 2023, coltrane2ny  
License: The MIT License (MIT)  
This software is provided "AS IS," under the terms of the MIT License. For more details, please refer to the [license file](https://github.com/coltrane2ny/Markdig.Wpf.ColorCode/blob/main/LICENSE).

#### MahApps.Metro
Copyright: 2023, Jan Karger, Dennis Daume, Brendan Forster, Paul Jenkins, Jake Ginnivan, Alex Mitchell
License: The MIT License (MIT)  
This software is provided "AS IS," under the terms of the MIT License. For more details, please refer to the [license file](https://github.com/MahApps/MahApps.Metro/blob/develop/LICENSE).

#### MaterialDesignThemes
Copyright: Year not specified, James Willock, James Willock
License: The MIT License (MIT)  
This software is provided "AS IS," under the terms of the MIT License. For more details, please refer to the [license file](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/blob/master/LICENSE).

#### Newtonsoft.Json
Copyright: 2007, James Newton-King  
License: The MIT License (MIT)  
This software is provided "AS IS," under the terms of the MIT License. For more details, please refer to the [license file](https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).

### For FetchUrlAPI.dll
The following third-party packages are utilized in the accompanying `FetchUrlAPI.dll`.

#### HtmlAgilityPack
Copyright: Year not specified, ZZZ Projects,Simon Mourrier,Jeff Klawiter,Stephan Grell
License: The MIT License (MIT)  
This software is provided "AS IS," under the terms of the MIT License. For more details, please refer to the [license file](https://github.com/zzzprojects/html-agility-pack/blob/master/LICENSE).

#### PdfPig
Copyright: January 2004, UglyToad  
License: Apache License 2.0  
This software is provided "AS IS," under the terms of the Apache License 2.0. For more details, please refer to the [license file](https://github.com/UglyToad/PdfPig/blob/master/LICENSE).

### For PythonInterpreterAPI.dll

#### pythonnet
Copyright: 2006-2021 the contributors of the Python.NET project
License: The MIT License (MIT)  
This software is provided "AS IS," under the terms of the MIT License. For more details, please refer to the [license file](https://github.com/pythonnet/pythonnet/blob/master/LICENSE).
