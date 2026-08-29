#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Pinball.Localization;
using UnityEditor;
using UnityEngine;

namespace Pinball.Ingame.Tutorial.Editor
{
    [CustomEditor(typeof(TutorialStepCatalogData))]
    public class TutorialStepCatalogDataEditor : UnityEditor.Editor
    {
        const string STEPS_PROPERTY = "_steps";
        const string ID_FIELD = "Id";
        const string KEY_FIELD = "Key";
        const string MASCOT_FIELD = "Mascot";
        const string HIGHLIGHT_FIELD = "Highlight";
        const string PLACEMENT_FIELD = "Placement";
        const string ENTRIES_PROPERTY = "_entries";
        const string ENTRY_KEY_FIELD = "Key";
        const string ENTRY_LANGUAGE_FIELD = "Language";
        const string ENTRY_TEXT_FIELD = "Text";
        const string TABLE_FILTER = "t:LocalizationTableData";
        const string SEARCH_LABEL = "검색";
        const string SEARCH_TOOLTIP = "단계 이름 · 키 · 텍스트로 검색합니다.";
        const string ADD_MISSING_LABEL = "누락 단계 추가";
        const string ADD_MISSING_TOOLTIP = "EStep에 있으나 목록에 없는 단계를 빈 항목으로 추가합니다.";
        const string SORT_LABEL = "단계 정렬";
        const string SORT_TOOLTIP = "EStep 선언 순서대로 목록을 정렬합니다.";
        const string PING_TABLE_LABEL = "테이블 보기";
        const string REFRESH_LABEL = "새로고침";
        const string REFRESH_TOOLTIP = "로컬라이제이션 테이블을 다시 읽어 옵니다.";
        const string EXPAND_ALL_LABEL = "모두 펼치기";
        const string COLLAPSE_ALL_LABEL = "모두 접기";
        const string TABLE_MISSING_MESSAGE = "LocalizationTableData 에셋을 찾지 못했습니다.";
        const string KEY_EMPTY_MESSAGE = "다이얼로그 키가 비어 있습니다.";
        const string KEY_MISSING_FORMAT = "테이블에 없는 키입니다: {0}";
        const string ADD_KEY_LABEL = "테이블에 키 추가";
        const string ADD_KEY_TOOLTIP = "이 키의 비어 있는 언어 항목을 테이블에 만들어 편집할 수 있게 합니다.";
        const string DUPLICATE_FORMAT = "중복된 단계 {0}개 — 런타임에는 먼저 나오는 항목만 적용됩니다.";
        const string MISSING_STEP_FORMAT = "목록에 없는 단계 {0}개 — 런타임에는 빈 다이얼로그가 표시됩니다.";
        const string SUMMARY_FORMAT = "단계 {0}/{1}개 · 표시 {2}개";
        const string EMPTY_RESULT_LABEL = "일치하는 단계가 없습니다.";
        const string EMPTY_TEXT_LABEL = "(비어 있음)";
        const string CURRENT_LANGUAGE_MARK = "▶ ";
        const string MASCOT_MISSING_LABEL = "없음";
        const string ADD_MISSING_UNDO_LABEL = "Add Tutorial Steps";
        const string SORT_UNDO_LABEL = "Sort Tutorial Steps";
        const string ADD_KEY_UNDO_LABEL = "Add Localization Key";
        const string EDIT_TEXT_UNDO_LABEL = "Edit Tutorial Dialog Text";
        const float PREVIEW_SIZE = 68f;
        const float LANGUAGE_LABEL_WIDTH = 74f;
        const float TEXT_AREA_HEIGHT = 38f;
        const float BUTTON_WIDTH = 104f;
        const float POPUP_WIDTH = 96f;
        const float HEADER_KEY_WIDTH = 220f;
        const int INVALID_INDEX = -1;

        static readonly Color EMPTY_TEXT_COLOR = new Color(1f, 0.85f, 0.5f, 1f);

        SerializedProperty _stepsProperty = null;
        SerializedObject _tableObject = null;
        SerializedProperty _entriesProperty = null;
        LocalizationTableData _table = null;
        ELanguage[] _languages = null;
        string[] _languageNameArr = null;
        Dictionary<string, int[]> _entryIndexByKey = null;
        int[] _stepIndexArr = null;
        bool[] _expandedArr = null;
        string _searchText = string.Empty;
        int _previewLanguageIndex = 0;
        int _duplicateCount = 0;
        int _missingCount = 0;
        int _visibleCount = 0;

        void OnEnable()
        {
            _stepsProperty = serializedObject.FindProperty(STEPS_PROPERTY);
            _entryIndexByKey = new Dictionary<string, int[]>();
            _stepIndexArr = new int[TutorialStepCatalogData.StepCount];
            _expandedArr = new bool[TutorialStepCatalogData.StepCount];

            LocalizationManager.EnsureInit();

            _languages = new ELanguage[LocalizationManager.LanguageCount];
            _languageNameArr = new string[_languages.Length];
            for (int i = 0; i < _languages.Length; ++i)
            {
                _languages[i] = LocalizationManager.GetLanguage(i);
                _languageNameArr[i] = LocalizationManager.GetDisplayName(_languages[i]);
            }

            _previewLanguageIndex = LocalizationManager.GetLanguageIndex(LocalizationManager.CurrentLanguage);
            BuildTable();
        }

        void OnDisable()
        {
            if (null != _entryIndexByKey)
                _entryIndexByKey.Clear();

            _stepsProperty = null;
            _tableObject = null;
            _entriesProperty = null;
            _table = null;
            _languages = null;
            _languageNameArr = null;
            _entryIndexByKey = null;
            _stepIndexArr = null;
            _expandedArr = null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            if (null != _tableObject)
                _tableObject.Update();

            BuildStepIndices();
            DrawToolbar();
            DrawSummary();

            if (null == _table)
                DrawTableMissing();

            DrawSteps();

            serializedObject.ApplyModifiedProperties();
        }

        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField(new GUIContent(SEARCH_LABEL, SEARCH_TOOLTIP), EditorStyles.miniLabel, GUILayout.Width(POPUP_WIDTH));
            _searchText = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField);
            _previewLanguageIndex = EditorGUILayout.Popup(_previewLanguageIndex, _languageNameArr, EditorStyles.toolbarPopup, GUILayout.Width(POPUP_WIDTH));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button(new GUIContent(ADD_MISSING_LABEL, ADD_MISSING_TOOLTIP), EditorStyles.toolbarButton, GUILayout.Width(BUTTON_WIDTH)))
                AddMissingSteps();

            if (GUILayout.Button(new GUIContent(SORT_LABEL, SORT_TOOLTIP), EditorStyles.toolbarButton, GUILayout.Width(BUTTON_WIDTH)))
                SortSteps();

            if (GUILayout.Button(EXPAND_ALL_LABEL, EditorStyles.toolbarButton, GUILayout.Width(BUTTON_WIDTH)))
                SetAllExpanded(true);

            if (GUILayout.Button(COLLAPSE_ALL_LABEL, EditorStyles.toolbarButton, GUILayout.Width(BUTTON_WIDTH)))
                SetAllExpanded(false);

            if (GUILayout.Button(PING_TABLE_LABEL, EditorStyles.toolbarButton, GUILayout.Width(BUTTON_WIDTH)))
                EditorGUIUtility.PingObject(_table);

            if (GUILayout.Button(new GUIContent(REFRESH_LABEL, REFRESH_TOOLTIP), EditorStyles.toolbarButton, GUILayout.Width(BUTTON_WIDTH)))
                BuildTable();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        void DrawSummary()
        {
            EditorGUILayout.LabelField(string.Format(SUMMARY_FORMAT, _stepsProperty.arraySize, TutorialStepCatalogData.StepCount, _visibleCount), EditorStyles.miniLabel);

            if (_missingCount > 0)
                EditorGUILayout.HelpBox(string.Format(MISSING_STEP_FORMAT, _missingCount), MessageType.Warning);

            if (_duplicateCount > 0)
                EditorGUILayout.HelpBox(string.Format(DUPLICATE_FORMAT, _duplicateCount), MessageType.Warning);
        }

        void DrawTableMissing()
        {
            EditorGUILayout.HelpBox(TABLE_MISSING_MESSAGE, MessageType.Warning);
        }

        void DrawSteps()
        {
            for (int i = 0; i < _stepIndexArr.Length; ++i)
            {
                int elementIndex = _stepIndexArr[i];
                if (elementIndex == INVALID_INDEX)
                    continue;

                SerializedProperty element = _stepsProperty.GetArrayElementAtIndex(elementIndex);
                if (!IsVisible(i, element))
                    continue;

                DrawStep(i, element);
            }

            if (_visibleCount <= 0)
                EditorGUILayout.LabelField(EMPTY_RESULT_LABEL, EditorStyles.centeredGreyMiniLabel);
        }

        void DrawStep(int stepIndex, SerializedProperty element)
        {
            SerializedProperty keyProperty = element.FindPropertyRelative(KEY_FIELD);
            string key = keyProperty.stringValue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawStepHeader(stepIndex, key);

            if (_expandedArr[stepIndex])
            {
                DrawStepFields(element);
                DrawStepTexts(keyProperty);
            }

            EditorGUILayout.EndVertical();
        }

        void DrawStepHeader(int stepIndex, string key)
        {
            EditorGUILayout.BeginHorizontal();
            _expandedArr[stepIndex] = EditorGUILayout.Foldout(_expandedArr[stepIndex], GetStepName(stepIndex), true, EditorStyles.foldout);
            EditorGUILayout.LabelField(key, EditorStyles.miniLabel, GUILayout.Width(HEADER_KEY_WIDTH));
            EditorGUILayout.EndHorizontal();

            if (_expandedArr[stepIndex])
                return;

            string preview = GetTableText(key, _previewLanguageIndex);
            bool isEmpty = string.IsNullOrEmpty(preview);
            Color previousColor = GUI.color;
            if (isEmpty)
                GUI.color = EMPTY_TEXT_COLOR;

            EditorGUILayout.LabelField(isEmpty ? EMPTY_TEXT_LABEL : preview, EditorStyles.wordWrappedMiniLabel);
            GUI.color = previousColor;
        }

        void DrawStepFields(SerializedProperty element)
        {
            EditorGUILayout.BeginHorizontal();
            DrawMascotPreview(element.FindPropertyRelative(MASCOT_FIELD));

            EditorGUILayout.BeginVertical();
            EditorGUILayout.PropertyField(element.FindPropertyRelative(KEY_FIELD));
            EditorGUILayout.PropertyField(element.FindPropertyRelative(MASCOT_FIELD));
            EditorGUILayout.PropertyField(element.FindPropertyRelative(HIGHLIGHT_FIELD));
            EditorGUILayout.PropertyField(element.FindPropertyRelative(PLACEMENT_FIELD));
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        static void DrawMascotPreview(SerializedProperty mascotProperty)
        {
            Rect rect = GUILayoutUtility.GetRect(PREVIEW_SIZE, PREVIEW_SIZE, GUILayout.Width(PREVIEW_SIZE), GUILayout.Height(PREVIEW_SIZE));
            Sprite mascot = mascotProperty.objectReferenceValue as Sprite;
            if (null == mascot)
            {
                EditorGUI.LabelField(rect, MASCOT_MISSING_LABEL, EditorStyles.centeredGreyMiniLabel);
                return;
            }

            Texture2D preview = AssetPreview.GetAssetPreview(mascot);
            if (null == preview)
                preview = AssetPreview.GetMiniThumbnail(mascot);

            if (null != preview)
                EditorGUI.DrawTextureTransparent(rect, preview, ScaleMode.ScaleToFit);
        }

        void DrawStepTexts(SerializedProperty keyProperty)
        {
            if (null == _table)
                return;

            string key = keyProperty.stringValue;
            if (string.IsNullOrEmpty(key))
            {
                EditorGUILayout.HelpBox(KEY_EMPTY_MESSAGE, MessageType.Info);
                return;
            }

            if (!HasAllLanguages(key))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(string.Format(KEY_MISSING_FORMAT, key), MessageType.Warning);
                if (GUILayout.Button(new GUIContent(ADD_KEY_LABEL, ADD_KEY_TOOLTIP), GUILayout.Width(BUTTON_WIDTH)))
                    AddTableKey(key);

                EditorGUILayout.EndHorizontal();
                return;
            }

            for (int i = 0; i < _languages.Length; ++i)
                DrawTextRow(key, i);
        }

        void DrawTextRow(string key, int languageIndex)
        {
            string label = _languageNameArr[languageIndex];
            if (languageIndex == LocalizationManager.GetLanguageIndex(LocalizationManager.CurrentLanguage))
                label = CURRENT_LANGUAGE_MARK + label;

            string text = GetTableText(key, languageIndex);

            EditorGUILayout.BeginHorizontal();
            Color previousColor = GUI.color;
            if (string.IsNullOrEmpty(text))
                GUI.color = EMPTY_TEXT_COLOR;

            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel, GUILayout.Width(LANGUAGE_LABEL_WIDTH));
            GUI.color = previousColor;

            EditorGUI.BeginChangeCheck();
            string edited = EditorGUILayout.TextArea(text, EditorStyles.textArea, GUILayout.MinHeight(TEXT_AREA_HEIGHT));
            if (EditorGUI.EndChangeCheck())
                SetTableText(key, languageIndex, edited);

            EditorGUILayout.EndHorizontal();
        }

        void BuildStepIndices()
        {
            _duplicateCount = 0;
            _missingCount = 0;
            for (int i = 0; i < _stepIndexArr.Length; ++i)
                _stepIndexArr[i] = INVALID_INDEX;

            for (int i = 0; i < _stepsProperty.arraySize; ++i)
            {
                int id = _stepsProperty.GetArrayElementAtIndex(i).FindPropertyRelative(ID_FIELD).enumValueIndex;
                if (id < 0 || id >= _stepIndexArr.Length)
                    continue;

                if (_stepIndexArr[id] != INVALID_INDEX)
                {
                    ++_duplicateCount;
                    continue;
                }

                _stepIndexArr[id] = i;
            }

            _visibleCount = 0;
            for (int i = 0; i < _stepIndexArr.Length; ++i)
            {
                if (_stepIndexArr[i] == INVALID_INDEX)
                {
                    ++_missingCount;
                    continue;
                }

                if (!IsVisible(i, _stepsProperty.GetArrayElementAtIndex(_stepIndexArr[i])))
                    continue;

                ++_visibleCount;
            }
        }

        bool IsVisible(int stepIndex, SerializedProperty element)
        {
            if (string.IsNullOrEmpty(_searchText))
                return true;

            if (Contains(GetStepName(stepIndex), _searchText))
                return true;

            string key = element.FindPropertyRelative(KEY_FIELD).stringValue;
            if (Contains(key, _searchText))
                return true;

            for (int i = 0; i < _languages.Length; ++i)
            {
                if (!Contains(GetTableText(key, i), _searchText))
                    continue;

                return true;
            }

            return false;
        }

        void SetAllExpanded(bool isExpanded)
        {
            for (int i = 0; i < _expandedArr.Length; ++i)
                _expandedArr[i] = isExpanded;
        }

        void AddMissingSteps()
        {
            for (int i = 0; i < _stepIndexArr.Length; ++i)
            {
                if (_stepIndexArr[i] != INVALID_INDEX)
                    continue;

                int index = _stepsProperty.arraySize;
                _stepsProperty.InsertArrayElementAtIndex(index);

                SerializedProperty element = _stepsProperty.GetArrayElementAtIndex(index);
                element.FindPropertyRelative(ID_FIELD).enumValueIndex = i;
                element.FindPropertyRelative(KEY_FIELD).stringValue = string.Empty;
                element.FindPropertyRelative(MASCOT_FIELD).objectReferenceValue = null;
                element.FindPropertyRelative(HIGHLIGHT_FIELD).enumValueIndex = (int)TutorialStepCatalogData.EHighlight.None;
                element.FindPropertyRelative(PLACEMENT_FIELD).enumValueIndex = (int)TutorialDialogView.EPlacement.Bottom;
            }

            serializedObject.ApplyModifiedProperties();
            Undo.SetCurrentGroupName(ADD_MISSING_UNDO_LABEL);
        }

        void SortSteps()
        {
            List<TutorialStepCatalogData.Step> sorted = new List<TutorialStepCatalogData.Step>(_stepsProperty.arraySize);
            for (int i = 0; i < _stepsProperty.arraySize; ++i)
            {
                SerializedProperty element = _stepsProperty.GetArrayElementAtIndex(i);
                TutorialStepCatalogData.Step step = new TutorialStepCatalogData.Step();
                step.Id = (TutorialStepCatalogData.EStep)element.FindPropertyRelative(ID_FIELD).enumValueIndex;
                step.Key = element.FindPropertyRelative(KEY_FIELD).stringValue;
                step.Mascot = element.FindPropertyRelative(MASCOT_FIELD).objectReferenceValue as Sprite;
                step.Highlight = (TutorialStepCatalogData.EHighlight)element.FindPropertyRelative(HIGHLIGHT_FIELD).enumValueIndex;
                step.Placement = (TutorialDialogView.EPlacement)element.FindPropertyRelative(PLACEMENT_FIELD).enumValueIndex;
                sorted.Add(step);
            }

            sorted.Sort(CompareStep);

            for (int i = 0; i < sorted.Count; ++i)
            {
                SerializedProperty element = _stepsProperty.GetArrayElementAtIndex(i);
                element.FindPropertyRelative(ID_FIELD).enumValueIndex = (int)sorted[i].Id;
                element.FindPropertyRelative(KEY_FIELD).stringValue = sorted[i].Key;
                element.FindPropertyRelative(MASCOT_FIELD).objectReferenceValue = sorted[i].Mascot;
                element.FindPropertyRelative(HIGHLIGHT_FIELD).enumValueIndex = (int)sorted[i].Highlight;
                element.FindPropertyRelative(PLACEMENT_FIELD).enumValueIndex = (int)sorted[i].Placement;
            }

            serializedObject.ApplyModifiedProperties();
            Undo.SetCurrentGroupName(SORT_UNDO_LABEL);
        }

        static int CompareStep(TutorialStepCatalogData.Step left, TutorialStepCatalogData.Step right)
        {
            return left.Id.CompareTo(right.Id);
        }

        bool HasAllLanguages(string key)
        {
            if (!_entryIndexByKey.TryGetValue(key, out int[] indexArr))
                return false;

            for (int i = 0; i < indexArr.Length; ++i)
            {
                if (indexArr[i] == INVALID_INDEX)
                    return false;
            }

            return true;
        }

        void AddTableKey(string key)
        {
            if (null == _entriesProperty)
                return;

            if (!_entryIndexByKey.TryGetValue(key, out int[] indexArr))
                indexArr = null;

            for (int i = 0; i < _languages.Length; ++i)
            {
                if (null != indexArr && indexArr[i] != INVALID_INDEX)
                    continue;

                int index = _entriesProperty.arraySize;
                _entriesProperty.InsertArrayElementAtIndex(index);

                SerializedProperty element = _entriesProperty.GetArrayElementAtIndex(index);
                element.FindPropertyRelative(ENTRY_KEY_FIELD).stringValue = key;
                element.FindPropertyRelative(ENTRY_LANGUAGE_FIELD).enumValueIndex = LocalizationManager.GetLanguageIndex(_languages[i]);
                element.FindPropertyRelative(ENTRY_TEXT_FIELD).stringValue = string.Empty;
            }

            _tableObject.ApplyModifiedProperties();
            Undo.SetCurrentGroupName(ADD_KEY_UNDO_LABEL);
            BuildEntryIndices();
        }

        string GetTableText(string key, int languageIndex)
        {
            if (null == _entriesProperty || string.IsNullOrEmpty(key))
                return string.Empty;

            if (!_entryIndexByKey.TryGetValue(key, out int[] indexArr))
                return string.Empty;

            if (languageIndex < 0 || languageIndex >= indexArr.Length)
                return string.Empty;

            int entryIndex = indexArr[languageIndex];
            if (entryIndex == INVALID_INDEX || entryIndex >= _entriesProperty.arraySize)
                return string.Empty;

            return _entriesProperty.GetArrayElementAtIndex(entryIndex).FindPropertyRelative(ENTRY_TEXT_FIELD).stringValue;
        }

        void SetTableText(string key, int languageIndex, string text)
        {
            if (null == _entriesProperty)
                return;

            if (!_entryIndexByKey.TryGetValue(key, out int[] indexArr))
                return;

            int entryIndex = indexArr[languageIndex];
            if (entryIndex == INVALID_INDEX || entryIndex >= _entriesProperty.arraySize)
                return;

            _entriesProperty.GetArrayElementAtIndex(entryIndex).FindPropertyRelative(ENTRY_TEXT_FIELD).stringValue = text;
            _tableObject.ApplyModifiedProperties();
            Undo.SetCurrentGroupName(EDIT_TEXT_UNDO_LABEL);
        }

        void BuildTable()
        {
            _table = FindTable();
            _tableObject = null;
            _entriesProperty = null;
            _entryIndexByKey.Clear();
            if (null == _table)
                return;

            _tableObject = new SerializedObject(_table);
            _entriesProperty = _tableObject.FindProperty(ENTRIES_PROPERTY);
            BuildEntryIndices();
        }

        void BuildEntryIndices()
        {
            _entryIndexByKey.Clear();
            if (null == _entriesProperty)
                return;

            for (int i = 0; i < _entriesProperty.arraySize; ++i)
            {
                SerializedProperty element = _entriesProperty.GetArrayElementAtIndex(i);
                string key = element.FindPropertyRelative(ENTRY_KEY_FIELD).stringValue;
                if (string.IsNullOrEmpty(key))
                    continue;

                int languageIndex = element.FindPropertyRelative(ENTRY_LANGUAGE_FIELD).enumValueIndex;
                if (languageIndex < 0 || languageIndex >= _languages.Length)
                    continue;

                if (!_entryIndexByKey.TryGetValue(key, out int[] indexArr))
                {
                    indexArr = new int[_languages.Length];
                    for (int k = 0; k < indexArr.Length; ++k)
                        indexArr[k] = INVALID_INDEX;

                    _entryIndexByKey.Add(key, indexArr);
                }

                indexArr[languageIndex] = i;
            }
        }

        static string GetStepName(int stepIndex)
        {
            return ((TutorialStepCatalogData.EStep)stepIndex).ToString();
        }

        static bool Contains(string source, string keyword)
        {
            if (string.IsNullOrEmpty(source))
                return false;

            return source.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static LocalizationTableData FindTable()
        {
            string[] guidArr = AssetDatabase.FindAssets(TABLE_FILTER);
            if (null == guidArr || guidArr.Length == 0)
                return null;

            return AssetDatabase.LoadAssetAtPath<LocalizationTableData>(AssetDatabase.GUIDToAssetPath(guidArr[0]));
        }
    }
}
#endif // UNITY_EDITOR
