using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.UI;

[CustomEditor(typeof(MLT_TextSwitch))]
public class MLT_TextSwitchEditor : Editor
{
    private bool isTranslating = false;
    private string sourceText = "";
    private SystemLanguage sourceLanguage = SystemLanguage.ChineseSimplified;
    private MLT_TranslationConfig config;
    private HttpClient httpClient;
    private System.Threading.CancellationTokenSource cancellationTokenSource;

    // 支持的目标语言列表
    private readonly Dictionary<SystemLanguage, string> supportedLanguages = new Dictionary<SystemLanguage, string>
    {
        { SystemLanguage.Afrikaans, "Afrikaans" },
        { SystemLanguage.Arabic, "Arabic" },
        { SystemLanguage.Basque, "Basque" },
        { SystemLanguage.Belarusian, "Belarusian" },
        { SystemLanguage.Bulgarian, "Bulgarian" },
        { SystemLanguage.Catalan, "Catalan" },
        { SystemLanguage.Chinese, "Chinese" },
        { SystemLanguage.ChineseSimplified, "Simplified Chinese" },
        { SystemLanguage.ChineseTraditional, "Traditional Chinese" },
        { SystemLanguage.Czech, "Czech" },
        { SystemLanguage.Danish, "Danish" },
        { SystemLanguage.Dutch, "Dutch" },
        { SystemLanguage.English, "English" },
        { SystemLanguage.Estonian, "Estonian" },
        { SystemLanguage.Faroese, "Faroese" },
        { SystemLanguage.Finnish, "Finnish" },
        { SystemLanguage.French, "French" },
        { SystemLanguage.German, "German" },
        { SystemLanguage.Greek, "Greek" },
        { SystemLanguage.Hebrew, "Hebrew" },
        { SystemLanguage.Hungarian, "Hungarian" },
        { SystemLanguage.Icelandic, "Icelandic" },
        { SystemLanguage.Indonesian, "Indonesian" },
        { SystemLanguage.Italian, "Italian" },
        { SystemLanguage.Japanese, "Japanese" },
        { SystemLanguage.Korean, "Korean" },
        { SystemLanguage.Latvian, "Latvian" },
        { SystemLanguage.Lithuanian, "Lithuanian" },
        { SystemLanguage.Norwegian, "Norwegian" },
        { SystemLanguage.Polish, "Polish" },
        { SystemLanguage.Portuguese, "Portuguese" },
        { SystemLanguage.Romanian, "Romanian" },
        { SystemLanguage.Russian, "Russian" },
        { SystemLanguage.SerboCroatian, "Serbo-Croatian" },
        { SystemLanguage.Slovak, "Slovak" },
        { SystemLanguage.Slovenian, "Slovenian" },
        { SystemLanguage.Spanish, "Spanish" },
        { SystemLanguage.Swedish, "Swedish" },
        { SystemLanguage.Thai, "Thai" },
        { SystemLanguage.Turkish, "Turkish" },
        { SystemLanguage.Ukrainian, "Ukrainian" },
        { SystemLanguage.Vietnamese, "Vietnamese" },
        { SystemLanguage.Hindi, "Hindi" },
        { SystemLanguage.Unknown, "Unknown" }
    };

    private void OnEnable()
    {
        config = MLT_TranslationConfig.Instance;
        // 从EditorPrefs中恢复sourceLanguage设置
        sourceLanguage = (SystemLanguage)EditorPrefs.GetInt("MLT_SourceLanguage", (int)SystemLanguage.ChineseSimplified);
        // 初始化httpClient
        httpClient = new HttpClient();
    }

    private void OnDisable()
    {
        // 清理资源
        if (httpClient != null)
        {
            httpClient.Dispose();
            httpClient = null;
        }
        if (cancellationTokenSource != null)
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MLT_TextSwitch textSwitch = (MLT_TextSwitch)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("AI Translation Tool", EditorStyles.boldLabel);

        // 配置状态
        if (!config.IsApiKeyValid())
        {
            EditorGUILayout.HelpBox("API Key not configured! Click 'Open Settings' to set up.", MessageType.Warning);
            if (GUILayout.Button("Open Settings"))
            {
                MLT_TranslationConfigWindow.ShowWindow();
            }
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Model: {config.preferredModel}");
            if (GUILayout.Button("Settings", GUILayout.Width(70)))
            {
                MLT_TranslationConfigWindow.ShowWindow();
            }
            EditorGUILayout.EndHorizontal();
        }

        // 源语言选择
        EditorGUI.BeginChangeCheck();
        sourceLanguage = (SystemLanguage)EditorGUILayout.EnumPopup("Source Language:", sourceLanguage);
        if (EditorGUI.EndChangeCheck())
        {
            // 保存sourceLanguage设置到EditorPrefs
            EditorPrefs.SetInt("MLT_SourceLanguage", (int)sourceLanguage);
        }

        // 获取Text组件的当前文本
        Text textComponent = textSwitch.GetComponent<Text>();
        if (textComponent != null)
        {
            sourceText = textComponent.text;
            EditorGUILayout.LabelField("Source Text:");
            EditorGUILayout.TextArea(sourceText, GUILayout.Height(40));
        }

        // 翻译按钮
        EditorGUILayout.BeginHorizontal();

        GUI.enabled = config.IsApiKeyValid() && !string.IsNullOrEmpty(sourceText) && !isTranslating;
        if (GUILayout.Button(isTranslating ? "Translating..." : "Translate to All Languages"))
        {
            TranslateToAllLanguages(textSwitch, sourceText);
        }
        GUI.enabled = true;

        // 取消按钮
        GUI.enabled = isTranslating;
        if (GUILayout.Button("Cancel", GUILayout.Width(60)))
        {
            CancelTranslation();
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        // 批量翻译按钮
        EditorGUILayout.Space();
        if (GUILayout.Button("Batch Translator Window"))
        {
            MLT_BatchTranslatorWindow.ShowWindow();
        }
    }

    private async void TranslateToAllLanguages(MLT_TextSwitch textSwitch, string sourceText)
    {
        isTranslating = true;
        cancellationTokenSource = new System.Threading.CancellationTokenSource();

        try
        {
            // 清空现有的语言文本列表
            textSwitch.languageTexts.Clear();

            // 使用配置中的目标语言
            var targetLanguages = config.targetLanguages;

            // 添加源语言文本
            textSwitch.languageTexts.Add(new MLT_TextSwitch.LanguageText
            {
                language = sourceLanguage,
                text = sourceText
            });

            Debug.Log("Translating to all languages in one request...");

            // 批量翻译到所有语言
            var translations = await TranslateToMultipleLanguages(sourceText, sourceLanguage, targetLanguages.ToList(), cancellationTokenSource.Token);

            if (translations != null && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                foreach (var kvp in translations)
                {
                    var lang = kvp.Key;
                    var translatedText = kvp.Value;

                    if (!string.IsNullOrEmpty(translatedText))
                    {
                        textSwitch.languageTexts.Add(new MLT_TextSwitch.LanguageText
                        {
                            language = lang,
                            text = translatedText
                        });
                    }
                }

                EditorUtility.SetDirty(textSwitch);
                Debug.Log("Translation completed!");
                EditorUtility.DisplayDialog("Success", "Translation completed successfully!", "OK");
            }
            else if (cancellationTokenSource.Token.IsCancellationRequested)
            {
                Debug.Log("Translation cancelled by user.");
                EditorUtility.DisplayDialog("Cancelled", "Translation was cancelled by user.", "OK");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Translation error: {e.Message}");
            EditorUtility.DisplayDialog("Error", $"Translation failed: {e.Message}", "OK");
        }
        finally
        {
            isTranslating = false;
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }
            Repaint();
        }
    }

    private void CancelTranslation()
    {
        if (cancellationTokenSource != null)
        {
            cancellationTokenSource.Cancel();
        }
    }

    private string GetLanguageName(SystemLanguage language)
    {
        // 如果在supportedLanguages字典中存在，使用字典中的值
        if (supportedLanguages.ContainsKey(language))
        {
            return supportedLanguages[language];
        }
        // 否则使用枚举名称
        return language.ToString();
    }

    private async Task<Dictionary<SystemLanguage, string>> TranslateToMultipleLanguages(string text, SystemLanguage fromLanguage, List<SystemLanguage> toLanguages, System.Threading.CancellationToken cancellationToken = default)
    {
        if (httpClient == null)
        {
            httpClient = new HttpClient();
        }

        try
        {
            // 过滤掉与源语言相同的目标语言
            var targetLangs = toLanguages.Where(lang => lang != fromLanguage).ToList();
            if (targetLangs.Count == 0)
            {
                return new Dictionary<SystemLanguage, string>();
            }

            // 构建语言列表字符串
            var languagesList = string.Join(", ", targetLangs.Select(lang => GetLanguageName(lang)));

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.openRouterApiKey}");
            httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://unity.com");
            httpClient.DefaultRequestHeaders.Add("X-Title", "Unity MLT Translator");

            var requestBody = new
            {
                model = config.preferredModel,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = $"You are a professional game translator. {config.gameContext} Translate the given UI text accurately while keeping it natural and appropriate for the game context. Return ONLY a valid JSON object with the translations, no explanations or additional text."
                    },
                    new
                    {
                        role = "user",
                        content = $"Translate the following text from {GetLanguageName(fromLanguage)} to these languages: {languagesList}.\n" +
                                  $"Context: This is UI text for a mobile casual game.\n\n" +
                                  $"Text to translate: \"{text}\"\n\n" +
                                  $"Return the translations as a JSON object where keys are the target language names and values are the translated texts. " +
                                  $"Example format: {{\"English\": \"Hello\", \"Japanese\": \"こんにちは\"}}"
                    }
                }
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("https://openrouter.ai/api/v1/chat/completions", content, cancellationToken);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                dynamic result = JsonConvert.DeserializeObject(responseString);
                string translationsJson = result.choices[0].message.content.ToString().Trim();

                // 解析返回的JSON
                var translationsDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(translationsJson);
                var resultDict = new Dictionary<SystemLanguage, string>();

                // 将语言名称映射回SystemLanguage枚举
                foreach (var kvp in translationsDict)
                {
                    var matchingLang = supportedLanguages.FirstOrDefault(x => x.Value == kvp.Key);
                    if (matchingLang.Key != SystemLanguage.Unknown && targetLangs.Contains(matchingLang.Key))
                    {
                        resultDict[matchingLang.Key] = kvp.Value;
                    }
                }

                return resultDict;
            }
            else
            {
                Debug.LogError($"API Error: {responseString}");
                return null;
            }
        }
        catch (System.OperationCanceledException)
        {
            Debug.Log("Translation request was cancelled.");
            return null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Request failed: {e.Message}");
            return null;
        }
    }

    // 保留原有的单个翻译方法，以备其他地方使用
    private async Task<string> TranslateText(string text, SystemLanguage fromLanguage, SystemLanguage toLanguage, System.Threading.CancellationToken cancellationToken = default)
    {
        // 如果源语言和目标语言相同，直接返回原文
        if (fromLanguage == toLanguage)
        {
            return text;
        }

        if (httpClient == null)
        {
            httpClient = new HttpClient();
        }

        try
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.openRouterApiKey}");
            httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://unity.com");
            httpClient.DefaultRequestHeaders.Add("X-Title", "Unity MLT Translator");

            var requestBody = new
            {
                model = config.preferredModel,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = $"You are a professional game translator. {config.gameContext} Translate the given UI text accurately while keeping it natural and appropriate for the game context. Only return the translated text without any explanation."
                    },
                    new
                    {
                        role = "user",
                        content = $"Translate the following text from {GetLanguageName(fromLanguage)} to {GetLanguageName(toLanguage)}. " +
                                  $"Context: This is UI text for a mobile casual game.\n\n" +
                                  $"Text to translate: {text}"
                    }
                }
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("https://openrouter.ai/api/v1/chat/completions", content, cancellationToken);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                dynamic result = JsonConvert.DeserializeObject(responseString);
                return result.choices[0].message.content.ToString().Trim();
            }
            else
            {
                Debug.LogError($"API Error: {responseString}");
                return null;
            }
        }
        catch (System.OperationCanceledException)
        {
            Debug.Log("Translation request was cancelled.");
            return null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Request failed: {e.Message}");
            return null;
        }
    }


}