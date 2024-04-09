# Translator

## Description

- This tool processes translation files with French as the base language and automatically translates to English (en),
  German (de), Spanish (es), Italian (it), Dutch (nl), and Portuguese (pt).

## Installation

1. Clone the repository:

```Shell 
   git clone git@github.com:ledoux38/Translator.git
   ```
2. Rename appsettings_sample.json to appsettings.json file in the root directory of the program and add your DeepL API key and path translation files:"

```Shell 
{
"DeepLApiKey": "xxxxx-xxx-xxxx-xxxx",
"BasePath": "~/workspace/xxxx/yyyyy/.../translation"
}
```

3. add link
   
exemple
```Shell
sudo ln -s /home/user/.../translator/start.sh /usr/local/bin/translate
```
now with the symbolic link you can now use the 'translate' command line to run the program
## Usage
1. Add a translation in the `fr.json` file.
2. Run the Translator program.
3. After the program successfully adds translations,
4. It's done :)
