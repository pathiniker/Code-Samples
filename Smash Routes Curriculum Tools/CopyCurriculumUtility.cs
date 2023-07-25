#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

public class CopyCurriculumUtility : OdinEditorWindow
{
    const string DEFAULT_CD_OBJECT_NAME = "Curriculum Definition";
    static readonly string[] VALID_EXTENSIONS =
    {
        "*.asset",
        "*.unity",
        "*.mat",
        "*.anim",
        "*.prefab",
        "*.controller",
    };
    
    string m_sourcePath = "";
    string m_targetPath = "";
    string m_originalCurriculumFilePath = "";
    string m_newCurriculumFilePath = "";
    string odin_copyFromInfo = "";
    CurriculumDefinition m_fromCurriculum = null;
    CurriculumDefinition m_newCD = null;

    List<string> m_checkedAssets = new List<string>();
    List<string> m_cachedSubDependencies = new List<string>();
    List<string> m_fileDestinations = new List<string>();
    Dictionary<string, string> m_fromToGuids = new Dictionary<string, string>();
    Dictionary<string, string> m_oldNewDestinations = new Dictionary<string, string>();

    bool m_validateStringInput(string s) { return !(s.IndexOfAny(Path.GetInvalidPathChars()) >= 0) && !(s.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0); }
    bool m_validateTargetLocation(string t) { return !(m_description.Length > 0 && Directory.Exists("Assets\\Content\\ORGANIZATIONS\\" + t)); }
    bool m_formIsValid() { return m_fromCurriculum != null && !string.IsNullOrEmpty(m_description) && m_validateStringInput(m_description) && m_validateTargetLocation(m_description); }
    bool m_copyCompleted() { return EditorPrefs.GetBool(ProjectConstants.EP_COPY_UTILITY_COMPLETED); }
    
    Color odin_green() { return ProjectConstants.ODIN_DEFAULT_GREEN; }
    Color odin_yellow() { return ProjectConstants.ODIN_DEFAULT_YELLOW; }
    
    [InfoBox("$odin_copyFromInfo"), 
        ValidateInput("m_validateStringInput", "Description contains illegal characters!", InfoMessageType.Warning),
        ValidateInput("m_validateTargetLocation", "Destination folder already exists!", InfoMessageType.Error)]
    [SerializeField, LabelText("New Curriculum Description"), HideIf("m_copyCompleted")]
    string m_description = "";

    IEnumerable odin_populate_curriculums_dropdown()
    {
        List<ValueDropdownItem> allCurriculums = new List<ValueDropdownItem>();

        string[] guids = AssetDatabase.FindAssets("t: CurriculumDefinition");
        foreach (string guid in guids)
        {
            CurriculumDefinition curriculum = AssetDatabase.LoadAssetAtPath<CurriculumDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            allCurriculums.Add(new ValueDropdownItem(curriculum.GetDescription(), curriculum));
        }

        return allCurriculums;
    }
    
    void generate_new_curriculum()
    {
        Application.logMessageReceived += handle_critical_error;
        
        string prepText = "Preparing to create new curriculum: \n" + m_description;
        prepText += "\n\n" + "Copying from base: \n" + m_fromCurriculum.GetDescription();
 
        if (EditorUtility.DisplayDialog("Proceed to Copy", prepText, "Proceed", "Cancel"))
        {
            Debug.Log("******* Creating directory... ******************");
            copy_files();
            save_and_refresh();
                
            Debug.Log("******* Building curriculum asset... ************");
            set_new_curriculum_asset();
            save_and_refresh();
                
            Debug.Log("******* Generating GUID dictionary... ***********");
            generate_guid_dictionary();
            save_and_refresh();
                
            Debug.Log("******* Replacing YAML GUIDS... ***************");
            replace_yaml_guids();

            Debug.Log("******* Finishing up... ******************");
            save_and_refresh();

            Debug.Log("******* Done! *******************************");
            
            string successMessage = "New curriculum created successfully!\n\n" + m_newCD.GetDescription();
            successMessage += "\n\n" + "*******************************************";
            successMessage += "\n" + "**IMPORTANT:\nUnity restart is required before editing new curriculum";
            successMessage += "\n" + "*******************************************";
            
            if (EditorUtility.DisplayDialog("Confirmation", successMessage, "RESTART NOW"))
                restart_unity_application();
        }
    }

    [PropertySpace]
    [Button("Generate New Curriculum", ButtonSizes.Gigantic), GUIColor("odin_green"), ShowIf("m_formIsValid"), HideIf("m_copyCompleted")]
    void prepare_generation()
    {
        run_sanity_check();
    }

    void run_sanity_check()
    {
        CurriculumDefinition cd = CurriculumEditor_Main.GetSelectedCurriculum();
        SR_SanityChecker.CurriculumSanityCheckUtility.GetSanityCheckSummary(cd, out int nonFatalErrors, out int fatalErrors);

        int totalErrors = nonFatalErrors + fatalErrors;
        
        if (fatalErrors == 0) // if fatal errors > 0, stop, else allow to proceed immediately
            generate_new_curriculum();
        else
        {
            string dialogue = ""; 
            dialogue += "WARNING: " + fatalErrors.ToString() + " critical errors found!";
            dialogue += "\nPlease fix these errors before duplicating.";

            if (EditorUtility.DisplayDialog("Sanity Check Results", dialogue, "View Sanity Check Summary", "Cancel"))
                SR_SanityChecker.CurriculumSanityCheckUtility.OpenSanityReport();
            else
                close_window();
        }
    }
    
    [PropertySpace]
    [HorizontalGroup("CloseButton", width: 165)]
    [Button("Close", ButtonSizes.Large), GUIColor("odin_yellow")]
    void close_window()
    {
        EditorApplication.delayCall += () => Close();
    }
    
    void copy_files()
    {
        m_sourcePath = Directory.GetParent(AssetDatabase.GetAssetPath(m_fromCurriculum)).ToString();
        m_targetPath = "Assets\\Content\\ORGANIZATIONS\\" + m_description;
        m_originalCurriculumFilePath = AssetDatabase.GetAssetPath(m_fromCurriculum).Replace("/", "\\");
        m_newCurriculumFilePath = m_originalCurriculumFilePath.Replace(m_sourcePath, m_targetPath);
        
        FileUtil.CopyFileOrDirectory(m_sourcePath, m_targetPath);
    }
    
    void set_new_curriculum_asset()
    {
        string path = m_newCurriculumFilePath.Replace("\\", "/");
        m_newCD = AssetDatabase.LoadAssetAtPath<CurriculumDefinition>(path);
        
        Debug.Assert(m_newCD != null);

        m_newCD.Editor_SetDescription(m_description);
        EditorUtility.SetDirty(m_newCD);
        AssetDatabase.SaveAssets();
        AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(m_newCD), DEFAULT_CD_OBJECT_NAME);

        m_newCurriculumFilePath = AssetDatabase.GetAssetPath(m_newCD);
        EditorPrefs.SetString(ProjectConstants.EP_CACHED_COPIED_CURRICULUM_PATH, m_newCurriculumFilePath);
    }
    
    void generate_guid_dictionary()
    {
        string sourcePath = Directory.GetParent(AssetDatabase.GetAssetPath(m_fromCurriculum)).ToString();
        
        m_fromToGuids.Clear();
        m_checkedAssets.Clear();
        add_to_guid_dictionary(AssetDatabase.GetAssetPath(m_fromCurriculum), AssetDatabase.GetAssetPath(m_newCD)); // we need to assert that these are added first
        
        foreach(string ext in VALID_EXTENSIONS)
            foreach (string file in Directory.GetFiles(sourcePath, ext, SearchOption.AllDirectories))
            {
                string newPath = file.Replace(sourcePath, m_targetPath);
                
                add_to_guid_dictionary(file, newPath);
                replicate_asset_dependencies(file);
            }

        save_and_refresh();
        find_sub_dependency_guids();
    }
    
    void replicate_asset_dependencies(string file)
    {
        string modTargetPath = m_targetPath.Replace("\\", "/");
        string modSourcePath = m_sourcePath.Replace("\\", "/");
        string cdGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_fromCurriculum));
        
        foreach (string d in AssetDatabase.GetDependencies(file, false))
        {
            if (d.Contains(".asset") && !d.Contains(modTargetPath) && !d.Contains(modSourcePath) && !m_checkedAssets.Contains(d))
            {
                m_checkedAssets.Add(d);

                if (!AssetDatabase.LoadAssetAtPath<CurriculumDefinition>(d))
                {
                    string fileName = Path.GetFileName(d);
                    string newFile = d.Replace("Assets/Content/ORGANIZATIONS/", "").Replace("/", "\\");
                    string newDirectory = (m_targetPath + "\\" + newFile).Replace("\\" + fileName, "");
                    string newPath = m_targetPath + "\\" + newFile;
                    string newGuidPath = newPath.Replace("\\", "/");
                    
                    if (!Directory.Exists(newDirectory))
                        Directory.CreateDirectory(newDirectory);
                    // This will skip duplicate files and prevent error,
                    // but let's default to being notified of the error (even if it means the process fails) (which it will) - Pat

                    /*
                    if (AssetDatabase.LoadAssetAtPath(newPath, typeof(object)))
                    {
                        //string errorMessage = "WARNING: File path already exists: " + newPath;
                        //errorMessage += "\nDependency of file: " + file;

                        //Debug.LogError(errorMessage); // Throwing a Debug ERROR type message will stop our process and throw a flag
                        //Debug.LogError("WARNING: File path already exists: " + newPath);
                        Debug.LogWarning("WARNING: File path already exists: " + newPath);
                        Debug.Log("<b>N:</b>" + newGuidPath);
                        Debug.Log("<b>O:</b> " + d);
                        Debug.Log("<b>Dependency of:</b> " + file);
                    } 
                    */
                    else
                   
                        FileUtil.CopyFileOrDirectory(d, newPath);
                    
                    add_to_guid_dictionary(d, newGuidPath);
                    m_cachedSubDependencies.Add(newGuidPath);
                }
                else
                    add_to_guid_dictionary(d, AssetDatabase.GetAssetPath(m_newCD)); // still add guid for referenced curriculum (necessary for Curriculum Helper)
            }
        }
    }

    void find_sub_dependency_guids() // this needs to be done AFTER dependency assets have been copied over and Unity has reimported them
    {
        string modTargetPath = m_targetPath.Replace("\\", "/");
        string modSourcePath = m_sourcePath.Replace("\\", "/");
        foreach (string cached in m_cachedSubDependencies)
        {
            foreach (string sub in AssetDatabase.GetDependencies(cached, false))
                if (!m_checkedAssets.Contains(sub) && sub.Contains(".asset"))
                {
                    string subFileName = Path.GetFileName(sub);
                    string newSubFile = sub.Replace("Assets/Content/ORGANIZATIONS/", "");
                    string newSubPath = m_targetPath.Replace("\\", "/") + "/" + newSubFile;
                    
                    string realSubPath = sub.Contains(modSourcePath) ? sub.Replace(modSourcePath, modTargetPath) : newSubPath;

                    add_to_guid_dictionary(sub, realSubPath);
                }
        }
    }

    void add_to_guid_dictionary(string oldPath, string newPath)
    {
        string oldGuid = AssetDatabase.AssetPathToGUID(oldPath);
        string newGuid = AssetDatabase.AssetPathToGUID(newPath);

        if (!string.IsNullOrEmpty(newGuid))
            if (!m_fromToGuids.ContainsKey(oldGuid))
                m_fromToGuids.Add(oldGuid, newGuid);
            else // trying to add duplicate GUID
            {
                Debug.Log("<b><i>Tried to add duplicate GUID for file: </i></b>" + oldPath);
            }
        else
            Debug.Log("Empty guid: " + newPath);
    }
    
    void replace_yaml_guids()
    {
        string sourcePath = Directory.GetParent(m_newCurriculumFilePath).ToString();

        foreach (string ext in VALID_EXTENSIONS)
            foreach (string fp in Directory.GetFiles(sourcePath, ext, SearchOption.AllDirectories))
                    find_and_replace_yaml_guids_at_path(fp);
    }

    void find_and_replace_yaml_guids_at_path(string path)
    {
        string yaml = File.ReadAllText(path);
        
        foreach (KeyValuePair<string, string> kvp in m_fromToGuids)
            if (yaml.Contains(kvp.Key))
                yaml = yaml.Replace(kvp.Key, kvp.Value);
        
        File.WriteAllText(path, yaml);
    }

    void save_and_refresh()
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    void restart_unity_application()
    {
        EditorPrefs.SetBool(ProjectConstants.EP_COPY_UTILITY_COMPLETED, true);
        System.Diagnostics.Process.Start(EditorApplication.applicationPath);
        EditorApplication.Exit(0);
    }

    void copy_to_clipboard(string text)
    {
        TextEditor te = new TextEditor();
        te.text = text;
        te.SelectAll();
        te.Copy();
    }

    void handle_critical_error(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            if (logString.ToLowerInvariant().Contains("layout"))
                return;

            string errorMessage = "";
            errorMessage += "Process ended early due to critical error!";
            errorMessage += "\nError type: " + type.ToString();

            errorMessage += "\n\n" + "MESSAGE:";
            errorMessage += "\n" + logString;

            string copyError = "";
            copyError += "Error when copying curriculum: " + m_fromCurriculum.GetDescription();
            copyError += "\nERROR TYPE: " + type.ToString();
            copyError += "\n\nMESSAGE: \n" + logString;
            copyError += "\n\nSTACK TRACE: \n" + stackTrace;
            copy_to_clipboard(copyError);

            if (EditorUtility.DisplayDialog("ERROR", errorMessage, "Copy error and close", "Close"))
            {
                if (Directory.Exists(m_targetPath))
                {
                    Debug.Log("Found folder: " + m_targetPath);
                    FileUtil.DeleteFileOrDirectory(m_targetPath);

                    save_and_refresh();
                }

                close_window();
            }
        }
    }

    void OnInspectorUpdate()
    {
        if (m_copyCompleted())
        {
            EditorPrefs.SetBool(ProjectConstants.EP_COPY_UTILITY_COMPLETED, false);

            var staleWindow = GetWindow<CurriculumEditor_Main>();
            staleWindow.CloseAllWindows();
            
            if (EditorUtility.DisplayDialog("Success", "Curriculum copy process complete!", "Close"))
            {
                EditorPrefs.SetString(ProjectConstants.EP_CACHED_COPIED_CURRICULUM_PATH, string.Empty);
                close_window();
            }
        }
    }

    protected override void Initialize()
    {
        if (!m_copyCompleted())
        {
            m_fromCurriculum = CurriculumEditor_Main.GetSelectedCurriculum();
            Debug.Assert(m_fromCurriculum != null);
            odin_copyFromInfo = "Copying from " + m_fromCurriculum.GetDescription();
        }
    }
}
#endif
