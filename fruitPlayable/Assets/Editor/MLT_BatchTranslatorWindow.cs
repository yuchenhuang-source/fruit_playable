using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// 批量翻译窗口
public class MLT_BatchTranslatorWindow : EditorWindow
{
    private Vector2 scrollPos;
    private List<TextTranslationItem> translationItems = new List<TextTranslationItem>();
    private bool isTranslating = false;
    private float progress = 0f;
    private string progressMessage = "";
    private SystemLanguage sourceLanguage = SystemLanguage.ChineseSimplified;
    private MLT_TranslationConfig config;
    
    // 取消功能相关
    private System.Threading.CancellationTokenSource cancellationTokenSource;
    private HttpClient httpClient;
    
    // 过滤选项
    private bool includeInactive = true;
    private bool onlyMissingTranslations = false;
    private string searchFilter = "";
    
    [Serializable]
    private class TextTranslationItem
    {
        public UnityEngine.UI.Text textComponent;
        public MLT_TextSwitch textSwitch;
        public string originalText;
        public bool selected;
        public string gameObjectPath;
        public bool hasExistingTranslations;
        
        public TextTranslationItem(UnityEngine.UI.Text text, MLT_TextSwitch switcher)
        {
            textComponent = text;
            textSwitch = switcher;
            originalText = text.text;
            selected = true;
            gameObjectPath = GetGameObjectPath(text.gameObject);
            hasExistingTranslations = switcher != null && switcher.languageTexts.Count > 0;
        }
        
        private static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
    }
    
    [MenuItem("Tools/MLT/MLT Batch Translator")]
    public static void ShowWindow()
    {
        var window = GetWindow<MLT_BatchTranslatorWindow>("MLT Batch Translator");
        window.minSize = new Vector2(600, 400);
    }
    
    private void OnEnable()
    {
        config = MLT_TranslationConfig.Instance;
        RefreshTextList();
        
        // 恢复保存的sourceLanguage设置
        if (EditorPrefs.HasKey("MLT_BatchTranslator_SourceLanguage"))
        {
            sourceLanguage = (SystemLanguage)EditorPrefs.GetInt("MLT_BatchTranslator_SourceLanguage", (int)SystemLanguage.ChineseSimplified);
        }
        
        // 初始化HttpClient
        if (httpClient == null)
        {
            httpClient = new HttpClient();
        }
    }
    
    private void OnGUI()
    {
        if (config == null)
        {
            EditorGUILayout.HelpBox("Configuration not found!", MessageType.Error);
            return;
        }
        
        // 标题
        EditorGUILayout.LabelField("Batch Text Translation", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // API状态检查
        if (!config.IsApiKeyValid())
        {
            EditorGUILayout.HelpBox("API Key not configured! Please set up in Translation Settings.", MessageType.Warning);
            if (GUILayout.Button("Open Settings"))
            {
                MLT_TranslationConfigWindow.ShowWindow();
            }
            EditorGUILayout.Space();
        }
        
        // 工具栏
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            RefreshTextList();
        }
        
        EditorGUILayout.Space();
        
        // 源语言选择
        EditorGUILayout.LabelField("Source Language:", GUILayout.Width(100));
        var newSourceLanguage = (SystemLanguage)EditorGUILayout.EnumPopup(sourceLanguage, EditorStyles.toolbarPopup, GUILayout.Width(150));
        if (newSourceLanguage != sourceLanguage)
        {
            sourceLanguage = newSourceLanguage;
            EditorPrefs.SetInt("MLT_BatchTranslator_SourceLanguage", (int)sourceLanguage);
        }
        
        EditorGUILayout.Space();
        
        // 过滤选项
        includeInactive = EditorGUILayout.ToggleLeft("Include Inactive", includeInactive, GUILayout.Width(110));
        onlyMissingTranslations = EditorGUILayout.ToggleLeft("Only Missing", onlyMissingTranslations, GUILayout.Width(100));
        
        EditorGUILayout.EndHorizontal();
        
        // 搜索框
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
        searchFilter = EditorGUILayout.TextField(searchFilter);
        if (GUILayout.Button("Clear", GUILayout.Width(50)))
        {
            searchFilter = "";
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // 选择工具
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select All", GUILayout.Width(80)))
        {
            foreach (var item in GetFilteredItems())
            {
                item.selected = true;
            }
        }
        if (GUILayout.Button("Select None", GUILayout.Width(80)))
        {
            foreach (var item in translationItems)
            {
                item.selected = false;
            }
        }
        if (GUILayout.Button("Invert", GUILayout.Width(60)))
        {
            foreach (var item in GetFilteredItems())
            {
                item.selected = !item.selected;
            }
        }
        
        EditorGUILayout.Space();
        
        var selectedCount = translationItems.Count(item => item.selected);
        EditorGUILayout.LabelField($"Selected: {selectedCount}/{translationItems.Count}");
        
        EditorGUILayout.EndHorizontal();
        
        // 文本列表
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
        
        var filteredItems = GetFilteredItems();
        
        if (filteredItems.Count == 0)
        {
            EditorGUILayout.LabelField("No Text components found matching the criteria.", EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            foreach (var item in filteredItems)
            {
                EditorGUILayout.BeginHorizontal();
                
                // 选择框
                item.selected = EditorGUILayout.Toggle(item.selected, GUILayout.Width(20));
                
                // GameObject路径
                EditorGUILayout.LabelField(item.gameObjectPath, GUILayout.Width(200));
                
                // 原始文本
                EditorGUILayout.LabelField(item.originalText, GUILayout.MinWidth(100));
                
                // 状态
                if (item.textSwitch == null)
                {
                    EditorGUILayout.LabelField("[No MLT_TextSwitch]", EditorStyles.miniLabel, GUILayout.Width(120));
                }
                else if (item.hasExistingTranslations)
                {
                    EditorGUILayout.LabelField($"[{item.textSwitch.languageTexts.Count} languages]", EditorStyles.miniLabel, GUILayout.Width(120));
                }
                else
                {
                    EditorGUILayout.LabelField("[No translations]", EditorStyles.miniLabel, GUILayout.Width(120));
                }
                
                // 操作按钮
                if (GUILayout.Button("Select", GUILayout.Width(50)))
                {
                    Selection.activeGameObject = item.textComponent.gameObject;
                    EditorGUIUtility.PingObject(item.textComponent.gameObject);
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        
        // 进度条
        if (isTranslating)
        {
            EditorGUILayout.Space();
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.Height(20)), progress, progressMessage);
        }
        
        // 操作按钮
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        
        // 添加MLT_TextSwitch组件
        if (GUILayout.Button("Add MLT_TextSwitch to Selected", GUILayout.Height(30)))
        {
            AddMLTTextSwitchToSelected();
        }
        
        // 翻译按钮
        GUI.enabled = config.IsApiKeyValid() && selectedCount > 0 && !isTranslating;
        if (GUILayout.Button(isTranslating ? "Translating..." : "Translate Selected", GUILayout.Height(30)))
        {
            TranslateSelected();
        }
        GUI.enabled = true;
        
        // 取消按钮
        GUI.enabled = isTranslating;
        if (GUILayout.Button("Cancel", GUILayout.Height(30), GUILayout.Width(80)))
        {
            CancelTranslation();
        }
        GUI.enabled = true;
        
        EditorGUILayout.EndHorizontal();
    }
    
    private List<TextTranslationItem> GetFilteredItems()
    {
        return translationItems.Where(item => 
        {
            // 搜索过滤
            if (!string.IsNullOrEmpty(searchFilter))
            {
                bool matchesFilter = item.gameObjectPath.ToLower().Contains(searchFilter.ToLower()) ||
                                   item.originalText.ToLower().Contains(searchFilter.ToLower());
                if (!matchesFilter) return false;
            }
            
            // 只显示缺少翻译的
            if (onlyMissingTranslations)
            {
                if (item.textSwitch != null && item.hasExistingTranslations)
                    return false;
            }
            
            return true;
        }).ToList();
    }
    
    private void RefreshTextList()
    {
        translationItems.Clear();
        
        // 查找场景中所有Text组件
        UnityEngine.UI.Text[] allTexts = includeInactive ? 
            Resources.FindObjectsOfTypeAll<UnityEngine.UI.Text>().Where(t => !EditorUtility.IsPersistent(t)).ToArray() :
            FindObjectsOfType<UnityEngine.UI.Text>();
        
        foreach (var text in allTexts)
        {
            if (text == null || string.IsNullOrEmpty(text.text)) continue;
            
            var textSwitch = text.GetComponent<MLT_TextSwitch>();
            translationItems.Add(new TextTranslationItem(text, textSwitch));
        }
        
        // 按路径排序
        translationItems = translationItems.OrderBy(item => item.gameObjectPath).ToList();
    }
    
    private void AddMLTTextSwitchToSelected()
    {
        int addedCount = 0;
        
        foreach (var item in translationItems.Where(i => i.selected && i.textSwitch == null))
        {
            item.textSwitch = item.textComponent.gameObject.AddComponent<MLT_TextSwitch>();
            item.hasExistingTranslations = false;
            EditorUtility.SetDirty(item.textComponent.gameObject);
            addedCount++;
        }
        
        if (addedCount > 0)
        {
            EditorUtility.DisplayDialog("Success", $"Added MLT_TextSwitch to {addedCount} objects.", "OK");
            RefreshTextList();
        }
        else
        {
            EditorUtility.DisplayDialog("Info", "No objects needed MLT_TextSwitch component.", "OK");
        }
    }
    
    private async void TranslateSelected()
    {
        var selectedItems = translationItems.Where(item => item.selected && item.textSwitch != null).ToList();
        
        if (selectedItems.Count == 0)
        {
            EditorUtility.DisplayDialog("Warning", "No items with MLT_TextSwitch selected for translation.", "OK");
            return;
        }
        
        isTranslating = true;
        progress = 0f;
        cancellationTokenSource = new System.Threading.CancellationTokenSource();
        
        try
        {
            int totalItems = selectedItems.Count;
            int currentItem = 0;
            
            foreach (var item in selectedItems)
            {
                if (cancellationTokenSource.Token.IsCancellationRequested)
                {
                    progressMessage = "Translation cancelled by user.";
                    break;
                }
                
                currentItem++;
                progress = (float)currentItem / totalItems;
                progressMessage = $"Translating {currentItem}/{totalItems}: {item.gameObjectPath}";
                
                // 清理现有翻译
                item.textSwitch.languageTexts.Clear();
                
                // 添加源语言
                if (System.Array.IndexOf(config.targetLanguages, sourceLanguage) == -1)
                {
                    item.textSwitch.languageTexts.Add(new MLT_TextSwitch.LanguageText
                    {
                        language = sourceLanguage,
                        text = item.originalText
                    });
                }
                
                // 翻译到目标语言
                foreach (var targetLang in config.targetLanguages)
                {
                    var translatedText = await TranslateText(item.originalText, sourceLanguage, targetLang, cancellationTokenSource.Token);
                    
                    if (!string.IsNullOrEmpty(translatedText))
                    {
                        item.textSwitch.languageTexts.Add(new MLT_TextSwitch.LanguageText
                        {
                            language = targetLang,
                            text = translatedText
                        });
                    }
                    
                    await Task.Delay(config.apiCallDelay);
                }
                
                EditorUtility.SetDirty(item.textSwitch);
                
                // 强制刷新UI
                if (currentItem % 5 == 0)
                {
                    Repaint();
                    await Task.Delay(100);
                }
            }
            
            if (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                EditorUtility.DisplayDialog("Success", $"Translated {totalItems} text components!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Cancelled", "Translation was cancelled by user.", "OK");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Translation failed: {e.Message}");
            EditorUtility.DisplayDialog("Error", $"Translation failed: {e.Message}", "OK");
        }
        finally
        {
            isTranslating = false;
            progress = 0f;
            progressMessage = "";
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }
            RefreshTextList();
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
    
    private async Task<string> TranslateText(string text, SystemLanguage sourceLang, SystemLanguage targetLang, System.Threading.CancellationToken cancellationToken = default)
    {
        if (sourceLang == targetLang) return text;
        if (string.IsNullOrWhiteSpace(text)) return text;
        
        var fromLang = GetLanguageCode(sourceLang);
        var toLang = GetLanguageCode(targetLang);
        
        try
        {
            // 根据是否有API Key决定使用哪个服务
            if (!string.IsNullOrEmpty(config.openRouterApiKey))
            {
                return await TranslateWithOpenRouter(httpClient, text, fromLang, toLang, cancellationToken);
            }
            else
            {
                return await TranslateWithMyMemory(httpClient, text, fromLang, toLang, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Translation cancelled by user.");
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"Translation error: {e.Message}");
            return null;
        }
    }
    
    private async Task<string> TranslateWithOpenRouter(HttpClient client, string text, string fromLang, string toLang, System.Threading.CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            model = config.preferredModel,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = $"You are a professional game translator. Translate the following text from {fromLang} to {toLang}. " +
                              $"Context: {config.gameContext}. " +
                              "Keep the translation natural and appropriate for gaming. " +
                              "Return ONLY the translated text without any explanation."
                },
                new
                {
                    role = "user",
                    content = text
                }
            }
        };
        
        var jsonContent = JsonConvert.SerializeObject(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.openRouterApiKey}");
        client.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/unity-game-translator");
        client.DefaultRequestHeaders.Add("X-Title", "Unity MLT Batch Translator");
        
        for (int retry = 0; retry < config.maxRetries; retry++)
        {
            try
            {
                var response = await client.PostAsync("https://openrouter.ai/api/v1/chat/completions", content, cancellationToken);
                var responseString = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = JObject.Parse(responseString);
                    return jsonResponse["choices"]?[0]?["message"]?["content"]?.ToString() ?? text;
                }
                
                Debug.LogError($"OpenRouter API error: {response.StatusCode} - {responseString}");
                
                if (retry < config.maxRetries - 1)
                {
                    await Task.Delay(1000 * (retry + 1));
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"OpenRouter translation error: {e.Message}");
                if (retry < config.maxRetries - 1)
                {
                    await Task.Delay(1000 * (retry + 1));
                }
            }
        }
        
        return text;
    }
    
    private async Task<string> TranslateWithMyMemory(HttpClient client, string text, string fromLang, string toLang, System.Threading.CancellationToken cancellationToken = default)
    {
        string contextHint = string.IsNullOrEmpty(config.gameContext) ? "" : $" (Context: {config.gameContext})";
        string textWithContext = text + contextHint;
        
        string encodedText = System.Net.WebUtility.UrlEncode(textWithContext);
        string url = $"https://api.mymemory.translated.net/get?q={encodedText}&langpair={fromLang}|{toLang}";
        
        for (int retry = 0; retry < config.maxRetries; retry++)
        {
            try
            {
                var response = await client.GetAsync(url, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jsonResponse = JObject.Parse(content);
                    
                    var translatedText = jsonResponse["responseData"]?["translatedText"]?.ToString();
                    
                    if (!string.IsNullOrEmpty(translatedText))
                    {
                        translatedText = translatedText.Replace(contextHint, "").Trim();
                        return translatedText;
                    }
                }
                
                if (retry < config.maxRetries - 1)
                {
                    await Task.Delay(500 * (retry + 1));
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"MyMemory translation error: {e.Message}");
                if (retry < config.maxRetries - 1)
                {
                    await Task.Delay(500 * (retry + 1));
                }
            }
        }
        
        return text;
    }
    
    private string GetLanguageCode(SystemLanguage language)
    {
        switch (language)
        {
            case SystemLanguage.English: return "en";
            case SystemLanguage.Chinese:
            case SystemLanguage.ChineseSimplified: return "zh-CN";
            case SystemLanguage.ChineseTraditional: return "zh-TW";
            case SystemLanguage.Japanese: return "ja";
            case SystemLanguage.Korean: return "ko";
            case SystemLanguage.Spanish: return "es";
            case SystemLanguage.French: return "fr";
            case SystemLanguage.German: return "de";
            case SystemLanguage.Italian: return "it";
            case SystemLanguage.Portuguese: return "pt";
            case SystemLanguage.Russian: return "ru";
            case SystemLanguage.Arabic: return "ar";
            case SystemLanguage.Dutch: return "nl";
            case SystemLanguage.Polish: return "pl";
            case SystemLanguage.Turkish: return "tr";
            case SystemLanguage.Thai: return "th";
            case SystemLanguage.Vietnamese: return "vi";
            case SystemLanguage.Indonesian: return "id";
            case SystemLanguage.Hebrew: return "he";
            case SystemLanguage.Swedish: return "sv";
            case SystemLanguage.Norwegian: return "no";
            case SystemLanguage.Danish: return "da";
            case SystemLanguage.Finnish: return "fi";
            case SystemLanguage.Czech: return "cs";
            case SystemLanguage.Hungarian: return "hu";
            case SystemLanguage.Greek: return "el";
            case SystemLanguage.Romanian: return "ro";
            case SystemLanguage.Bulgarian: return "bg";
            case SystemLanguage.Ukrainian: return "uk";
            case SystemLanguage.Hindi: return "hi";
            default: return "en";
        }
    }
}