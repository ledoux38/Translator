# Translator

## Description

- This tool processes translation files with French as the base language and automatically translates to English (en),
  German (de), Spanish (es), Italian (it), Dutch (nl), and Portuguese (pt).

## Installation

1. Clone the repository:

```Shell 
   git clone git@github.com:ledoux38/Translator.git
   ```
2. Create an appsettings.json file in the root directory of the program and add your DeepL API key:"

```Shell 
{
"DeepLApiKey": "xxxxx-xxx-xxxx-xxxx",
"BasePath": "~/workspace/synergee/synergee/front/src/assets/translation"
}
```
3. Install the required NuGet packages. (nuget restore)

## Usage
1. Add a translation in the `fr.json` file.
2. Run the Translator program.
3. After the program successfully adds translations,
4. It's done :)
