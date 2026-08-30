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
        const string FLOW_FIELD = "Flow";
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
        const string SEARCH_TOOLTIP = "단계 이름 · 키 · 본문 · 2페이지 본문으로 검색합니다.";
        const string ADD_MISSING_LABEL = "누락 단계 추가";
        const string ADD_MISSING_TOOLTIP = "EStep에 있으나 목록에 없는 단계를 미분류로 추가합니다.";
        const string REGROUP_LABEL = "그룹별 정리";
        const string REGROUP_TOOLTIP = "흐름별로 항목을 모읍니다. 흐름 안의 재생 순서는 그대로 유지합니다.";
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
        const string UNASSIGNED_MESSAGE = "흐름이 지정되지 않은 단계입니다. 재생되지 않으므로 흐름을 지정하세요.";
        const string SUMMARY_FORMAT = "단계 {0}/{1}개 · 표시 {2}개";
        const string EMPTY_RESULT_LABEL = "일치하는 단계가 없습니다.";
        const string EMPTY_SECTION_LABEL = "비어 있음";
        const string EMPTY_TEXT_LABEL = "(비어 있음)";
        const string CURRENT_LANGUAGE_MARK = "▶ ";
        const string MASCOT_MISSING_LABEL = "없음";
        const string ORDERED_HINT = "인스펙터에 놓인 순서대로 재생됩니다.";
        const string TRIGGERED_HINT = "플레이 상황이 발동 시점을 정합니다. 순서는 코드가 결정합니다.";
        const string SECOND_PAGE_HEADER_FORMAT = "2페이지 · {0}";
        const string SECOND_PAGE_ADD_LABEL = "2페이지 추가";
        const string SECOND_PAGE_REMOVE_LABEL = "2페이지 삭제";
        const string SECOND_PAGE_EMPTY_LABEL = "2페이지 없이 한 장으로 표시됩니다.";
        const string SECOND_PAGE_BADGE = "2p";
        const string REMOVE_STEP_LABEL = "✕";
        const string REMOVE_STEP_TOOLTIP = "이 단계를 카탈로그에서 제거합니다.";
        const string REMOVE_STEP_TITLE = "단계 제거";
        const string REMOVE_STEP_FORMAT = "'{0}' 단계를 카탈로그에서 제거합니다.\n로컬라이즈 텍스트('{1}', '{2}')도 함께 지울지 고르세요.";
        const string REMOVE_STEP_ONLY = "단계만 제거";
        const string REMOVE_STEP_WITH_TEXT = "텍스트까지 제거";
        const string REMOVE_STEP_CANCEL = "취소";
        const string REMOVE_CONFIRM_TITLE = "2페이지 삭제";
        const string REMOVE_CONFIRM_FORMAT = "'{0}' 항목을 모든 언어에서 지웁니다. 되돌릴 수 없습니다.";
        const string REMOVE_CONFIRM_OK = "삭제";
        const string REMOVE_CONFIRM_CANCEL = "취소";
        const string FLOW_HEADER_FORMAT = "{0}  ({1}단계)";
        const string ORDINAL_FORMAT = "{0}.";
        const string MOVE_UP_LABEL = "▲";
        const string MOVE_DOWN_LABEL = "▼";
        const string ADD_MISSING_UNDO_LABEL = "Add Tutorial Steps";
        const string REGROUP_UNDO_LABEL = "Regroup Tutorial Steps";
        const string MOVE_UNDO_LABEL = "Reorder Tutorial Steps";
        const string ADD_KEY_UNDO_LABEL = "Add Localization Key";
        const string REMOVE_KEY_UNDO_LABEL = "Remove Localization Key";
        const string REMOVE_STEP_UNDO_LABEL = "Remove Tutorial Step";
        const string EDIT_TEXT_UNDO_LABEL = "Edit Tutorial Dialog Text";
        const float PREVIEW_SIZE = 68f;
        const float LANGUAGE_LABEL_WIDTH = 74f;
        const float TEXT_AREA_HEIGHT = 38f;
        const float BUTTON_WIDTH = 104f;
        const float POPUP_WIDTH = 96f;
        const float HEADER_KEY_WIDTH = 200f;
        const float ORDINAL_WIDTH = 24f;
        const float MOVE_BUTTON_WIDTH = 22f;
        const float BADGE_WIDTH = 24f;
        const float SECTION_SPACING = 6f;
        const int INVALID_INDEX = -1;

        static readonly Color EMPTY_TEXT_COLOR = new Color(1f, 0.85f, 0.5f, 1f);
        static readonly Color BADGE_COLOR = new Color(0.55f, 0.85f, 1f, 1f);
        static readonly TutorialStepCatalogData.EStep[] STEP_VALUE_ARR = (TutorialStepCatalogData.EStep[])Enum.GetValues(typeof(TutorialStepCatalogData.EStep));

        SerializedProperty _stepsProperty = null;
        SerializedObject _tableObject = null;
        SerializedProperty _entriesProperty = null;
        LocalizationTableData _table = null;
        ELanguage[] _languages = null;
        string[] _languageNameArr = null;
        Dictionary<string, int[]> _entryIndexByKey = null;
        List<int>[] _flowIndexListArr = null;
        bool[] _flowExpandedArr = null;
        int[] _stepIndexArr = null;
        bool[] _expandedArr = null;
        GUIStyle _sectionStyle = null;
        string _searchText = string.Empty;
        int _previewLanguageIndex = 0;
        int _duplicateCount = 0;
        int _missingCount = 0;
        int _visibleCount = 0;
        int _moveFromIndex = INVALID_INDEX;
        int _moveToIndex = INVALID_INDEX;
        int _removeIndex = INVALID_INDEX;

        void OnEnable()
        {
            _stepsProperty = serializedObject.FindProperty(STEPS_PROPERTY);
            _entryIndexByKey = new Dictionary<string, int[]>();
            _stepIndexArr = new int[TutorialStepCatalogData.StepCount];
            _expandedArr = new bool[TutorialStepCatalogData.StepCount];
            _flowExpandedArr = new bool[TutorialStepCatalogData.FlowCount];
            _flowIndexListArr = new List<int>[TutorialStepCatalogData.FlowCount];
            for (int i = 0; i < _flowIndexListArr.Length; ++i)
            {
                _flowIndexListArr[i] = new List<int>();
                _flowExpandedArr[i] = true;
            }

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
            _flowIndexListArr = null;
            _flowExpandedArr = null;
            _stepIndexArr = null;
            _expandedArr = null;
            _sectionStyle = null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            if (null != _tableObject)
                _tableObject.Update();

            EnsureStyles();
            BuildStepIndices();
            DrawToolbar();
            DrawSummary();

            if (null == _table)
                EditorGUILayout.HelpBox(TABLE_MISSING_MESSAGE, MessageType.Warning);

            for (int i = 0; i < _flowIndexListArr.Length; ++i)
                DrawFlowSection((TutorialStepCatalogData.EFlow)i);

            if (_visibleCount <= 0)
                EditorGUILayout.LabelField(EMPTY_RESULT_LABEL, EditorStyles.centeredGreyMiniLabel);

            ApplyPendingMove();
            ApplyPendingRemove();
            serializedObject.ApplyModifiedProperties();
        }

        void EnsureStyles()
        {
            if (null != _sectionStyle)
                return;

            _sectionStyle = new GUIStyle(EditorStyles.foldout);
            _sectionStyle.fontStyle = FontStyle.Bold;
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

            if (GUILayout.Button(new GUIContent(REGROUP_LABEL, REGROUP_TOOLTIP), EditorStyles.toolbarButton, GUILayout.Width(BUTTON_WIDTH)))
                RegroupSteps();

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

        void DrawFlowSection(TutorialStepCatalogData.EFlow flow)
        {
            int flowIndex = (int)flow;
            List<int> indexList = _flowIndexListArr[flowIndex];
            if (indexList.Count <= 0 && flow == TutorialStepCatalogData.EFlow.Unassigned)
                return;

            if (!string.IsNullOrEmpty(_searchText) && CountVisible(indexList) <= 0)
                return;

            EditorGUILayout.Space(SECTION_SPACING);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            string header = string.Format(FLOW_HEADER_FORMAT, GetFlowName(flow), indexList.Count);
            _flowExpandedArr[flowIndex] = EditorGUILayout.Foldout(_flowExpandedArr[flowIndex], header, true, _sectionStyle);

            if (_flowExpandedArr[flowIndex])
            {
                if (flow == TutorialStepCatalogData.EFlow.Unassigned)
                    EditorGUILayout.HelpBox(UNASSIGNED_MESSAGE, MessageType.Warning);
                else
                    EditorGUILayout.LabelField(GetFlowHint(flow), EditorStyles.wordWrappedMiniLabel);

                if (indexList.Count <= 0)
                    EditorGUILayout.LabelField(EMPTY_SECTION_LABEL, EditorStyles.centeredGreyMiniLabel);

                for (int i = 0; i < indexList.Count; ++i)
                    DrawStep(indexList, i, flow);
            }

            EditorGUILayout.EndVertical();
        }

        void DrawStep(List<int> indexList, int position, TutorialStepCatalogData.EFlow flow)
        {
            SerializedProperty element = _stepsProperty.GetArrayElementAtIndex(indexList[position]);
            int stepIndex = element.FindPropertyRelative(ID_FIELD).enumValueIndex;
            if (!IsVisible(stepIndex, element))
                return;

            string key = element.FindPropertyRelative(KEY_FIELD).stringValue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawStepHeader(indexList, position, stepIndex, key, flow);

            if (_expandedArr[stepIndex])
            {
                DrawStepFields(element);
                DrawStepTexts(key);
                DrawSecondPage(key);
            }

            EditorGUILayout.EndVertical();
        }

        void DrawStepHeader(List<int> indexList, int position, int stepIndex, string key, TutorialStepCatalogData.EFlow flow)
        {
            bool isOrdered = TutorialStepCatalogData.IsOrderedFlow(flow);

            EditorGUILayout.BeginHorizontal();
            if (isOrdered)
                EditorGUILayout.LabelField(string.Format(ORDINAL_FORMAT, position + 1), EditorStyles.miniBoldLabel, GUILayout.Width(ORDINAL_WIDTH));

            _expandedArr[stepIndex] = EditorGUILayout.Foldout(_expandedArr[stepIndex], GetStepName(stepIndex), true, EditorStyles.foldout);
            DrawSecondPageBadge(key);
            EditorGUILayout.LabelField(key, EditorStyles.miniLabel, GUILayout.Width(HEADER_KEY_WIDTH));

            if (isOrdered)
                DrawMoveButtons(indexList, position);

            if (GUILayout.Button(new GUIContent(REMOVE_STEP_LABEL, REMOVE_STEP_TOOLTIP), EditorStyles.miniButton, GUILayout.Width(MOVE_BUTTON_WIDTH)))
                ConfirmRemoveStep(indexList[position], stepIndex, key);

            EditorGUILayout.EndHorizontal();

            if (_expandedArr[stepIndex])
                return;

            if (!isOrdered)
            {
                string note = GetTriggerNote(stepIndex);
                if (!string.IsNullOrEmpty(note))
                    EditorGUILayout.LabelField(note, EditorStyles.miniLabel);
            }

            string preview = GetTableText(key, _previewLanguageIndex);
            bool isEmpty = string.IsNullOrEmpty(preview);
            Color previousColor = GUI.color;
            if (isEmpty)
                GUI.color = EMPTY_TEXT_COLOR;

            EditorGUILayout.LabelField(isEmpty ? EMPTY_TEXT_LABEL : preview, EditorStyles.wordWrappedMiniLabel);
            GUI.color = previousColor;
        }

        void DrawSecondPageBadge(string key)
        {
            if (!HasAllLanguages(TutorialStepCatalogData.GetSecondPageKey(key)))
            {
                GUILayout.Space(BADGE_WIDTH);
                return;
            }

            Color previousColor = GUI.color;
            GUI.color = BADGE_COLOR;
            EditorGUILayout.LabelField(SECOND_PAGE_BADGE, EditorStyles.miniBoldLabel, GUILayout.Width(BADGE_WIDTH));
            GUI.color = previousColor;
        }

        void DrawMoveButtons(List<int> indexList, int position)
        {
            EditorGUI.BeginDisabledGroup(position <= 0);
            if (GUILayout.Button(MOVE_UP_LABEL, EditorStyles.miniButtonLeft, GUILayout.Width(MOVE_BUTTON_WIDTH)))
                RequestMove(indexList[position], indexList[position - 1]);

            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(position >= indexList.Count - 1);
            if (GUILayout.Button(MOVE_DOWN_LABEL, EditorStyles.miniButtonRight, GUILayout.Width(MOVE_BUTTON_WIDTH)))
                RequestMove(indexList[position], indexList[position + 1]);

            EditorGUI.EndDisabledGroup();
        }

        void RequestMove(int fromIndex, int toIndex)
        {
            _moveFromIndex = fromIndex;
            _moveToIndex = toIndex;
        }

        void ApplyPendingMove()
        {
            if (_moveFromIndex == INVALID_INDEX)
                return;

            _stepsProperty.MoveArrayElement(_moveFromIndex, _moveToIndex);
            _moveFromIndex = INVALID_INDEX;
            _moveToIndex = INVALID_INDEX;
            Undo.SetCurrentGroupName(MOVE_UNDO_LABEL);
            Repaint();
        }

        void RequestRemove(int elementIndex)
        {
            _removeIndex = elementIndex;
        }

        void ApplyPendingRemove()
        {
            if (_removeIndex == INVALID_INDEX)
                return;

            _stepsProperty.DeleteArrayElementAtIndex(_removeIndex);
            _removeIndex = INVALID_INDEX;
            Undo.SetCurrentGroupName(REMOVE_STEP_UNDO_LABEL);
            Repaint();
        }

        void ConfirmRemoveStep(int elementIndex, int stepIndex, string key)
        {
            string secondKey = TutorialStepCatalogData.GetSecondPageKey(key);
            int choice = EditorUtility.DisplayDialogComplex(
                REMOVE_STEP_TITLE,
                string.Format(REMOVE_STEP_FORMAT, GetStepName(stepIndex), key, secondKey),
                REMOVE_STEP_ONLY,
                REMOVE_STEP_CANCEL,
                REMOVE_STEP_WITH_TEXT);

            if (choice == 1)
                return;

            if (choice == 2)
            {
                DeleteTableKey(key);
                DeleteTableKey(secondKey);
            }

            RequestRemove(elementIndex);
        }

        void DrawStepFields(SerializedProperty element)
        {
            EditorGUILayout.BeginHorizontal();
            DrawMascotPreview(element.FindPropertyRelative(MASCOT_FIELD));

            EditorGUILayout.BeginVertical();
            EditorGUILayout.PropertyField(element.FindPropertyRelative(FLOW_FIELD));
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

        void DrawStepTexts(string key)
        {
            if (null == _table)
                return;

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

        void DrawSecondPage(string key)
        {
            if (null == _table || string.IsNullOrEmpty(key))
                return;

            string secondKey = TutorialStepCatalogData.GetSecondPageKey(key);
            bool hasSecondPage = HasAllLanguages(secondKey);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(string.Format(SECOND_PAGE_HEADER_FORMAT, secondKey), EditorStyles.miniBoldLabel);
            if (!hasSecondPage && GUILayout.Button(SECOND_PAGE_ADD_LABEL, EditorStyles.miniButton, GUILayout.Width(BUTTON_WIDTH)))
                AddTableKey(secondKey);

            if (hasSecondPage && GUILayout.Button(SECOND_PAGE_REMOVE_LABEL, EditorStyles.miniButton, GUILayout.Width(BUTTON_WIDTH)))
                RemoveTableKey(secondKey);

            EditorGUILayout.EndHorizontal();

            if (!hasSecondPage)
            {
                EditorGUILayout.LabelField(SECOND_PAGE_EMPTY_LABEL, EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.EndVertical();
                return;
            }

            for (int i = 0; i < _languages.Length; ++i)
                DrawTextRow(secondKey, i);

            EditorGUILayout.EndVertical();
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

            for (int i = 0; i < _flowIndexListArr.Length; ++i)
                _flowIndexListArr[i].Clear();

            for (int i = 0; i < _stepsProperty.arraySize; ++i)
            {
                SerializedProperty element = _stepsProperty.GetArrayElementAtIndex(i);
                int id = element.FindPropertyRelative(ID_FIELD).enumValueIndex;
                if (id < 0 || id >= _stepIndexArr.Length)
                    continue;

                if (_stepIndexArr[id] != INVALID_INDEX)
                {
                    ++_duplicateCount;
                    continue;
                }

                _stepIndexArr[id] = i;

                int flow = element.FindPropertyRelative(FLOW_FIELD).enumValueIndex;
                if (flow < 0 || flow >= _flowIndexListArr.Length)
                    flow = (int)TutorialStepCatalogData.EFlow.Unassigned;

                _flowIndexListArr[flow].Add(i);
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

        int CountVisible(List<int> indexList)
        {
            int visible = 0;
            for (int i = 0; i < indexList.Count; ++i)
            {
                SerializedProperty element = _stepsProperty.GetArrayElementAtIndex(indexList[i]);
                if (!IsVisible(element.FindPropertyRelative(ID_FIELD).enumValueIndex, element))
                    continue;

                ++visible;
            }

            return visible;
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

            string secondKey = TutorialStepCatalogData.GetSecondPageKey(key);
            for (int i = 0; i < _languages.Length; ++i)
            {
                if (Contains(GetTableText(key, i), _searchText))
                    return true;

                if (Contains(GetTableText(secondKey, i), _searchText))
                    return true;
            }

            return false;
        }

        void SetAllExpanded(bool isExpanded)
        {
            for (int i = 0; i < _expandedArr.Length; ++i)
                _expandedArr[i] = isExpanded;

            for (int i = 0; i < _flowExpandedArr.Length; ++i)
                _flowExpandedArr[i] = true;
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
                element.FindPropertyRelative(FLOW_FIELD).enumValueIndex = (int)TutorialStepCatalogData.EFlow.Unassigned;
                element.FindPropertyRelative(KEY_FIELD).stringValue = string.Empty;
                element.FindPropertyRelative(MASCOT_FIELD).objectReferenceValue = null;
                element.FindPropertyRelative(HIGHLIGHT_FIELD).enumValueIndex = (int)TutorialStepCatalogData.EHighlight.None;
                element.FindPropertyRelative(PLACEMENT_FIELD).enumValueIndex = (int)TutorialDialogView.EPlacement.Bottom;
            }

            serializedObject.ApplyModifiedProperties();
            Undo.SetCurrentGroupName(ADD_MISSING_UNDO_LABEL);
        }

        void RegroupSteps()
        {
            List<TutorialStepCatalogData.Step> grouped = new List<TutorialStepCatalogData.Step>(_stepsProperty.arraySize);
            for (int flow = 0; flow < _flowIndexListArr.Length; ++flow)
            {
                List<int> indexList = _flowIndexListArr[flow];
                for (int i = 0; i < indexList.Count; ++i)
                    grouped.Add(ReadStep(_stepsProperty.GetArrayElementAtIndex(indexList[i])));
            }

            for (int i = 0; i < grouped.Count && i < _stepsProperty.arraySize; ++i)
                WriteStep(_stepsProperty.GetArrayElementAtIndex(i), grouped[i]);

            serializedObject.ApplyModifiedProperties();
            Undo.SetCurrentGroupName(REGROUP_UNDO_LABEL);
        }

        static TutorialStepCatalogData.Step ReadStep(SerializedProperty element)
        {
            TutorialStepCatalogData.Step step = new TutorialStepCatalogData.Step();
            step.Id = (TutorialStepCatalogData.EStep)element.FindPropertyRelative(ID_FIELD).intValue;
            step.Flow = (TutorialStepCatalogData.EFlow)element.FindPropertyRelative(FLOW_FIELD).intValue;
            step.Key = element.FindPropertyRelative(KEY_FIELD).stringValue;
            step.Mascot = element.FindPropertyRelative(MASCOT_FIELD).objectReferenceValue as Sprite;
            step.Highlight = (TutorialStepCatalogData.EHighlight)element.FindPropertyRelative(HIGHLIGHT_FIELD).intValue;
            step.Placement = (TutorialDialogView.EPlacement)element.FindPropertyRelative(PLACEMENT_FIELD).intValue;
            return step;
        }

        static void WriteStep(SerializedProperty element, TutorialStepCatalogData.Step step)
        {
            element.FindPropertyRelative(ID_FIELD).intValue = (int)step.Id;
            element.FindPropertyRelative(FLOW_FIELD).intValue = (int)step.Flow;
            element.FindPropertyRelative(KEY_FIELD).stringValue = step.Key;
            element.FindPropertyRelative(MASCOT_FIELD).objectReferenceValue = step.Mascot;
            element.FindPropertyRelative(HIGHLIGHT_FIELD).intValue = (int)step.Highlight;
            element.FindPropertyRelative(PLACEMENT_FIELD).intValue = (int)step.Placement;
        }

        bool HasAllLanguages(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

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

        void RemoveTableKey(string key)
        {
            if (!EditorUtility.DisplayDialog(REMOVE_CONFIRM_TITLE, string.Format(REMOVE_CONFIRM_FORMAT, key), REMOVE_CONFIRM_OK, REMOVE_CONFIRM_CANCEL))
                return;

            DeleteTableKey(key);
        }

        void DeleteTableKey(string key)
        {
            if (null == _entriesProperty)
                return;

            if (!_entryIndexByKey.TryGetValue(key, out int[] indexArr))
                return;

            List<int> removeList = new List<int>(indexArr.Length);
            for (int i = 0; i < indexArr.Length; ++i)
            {
                if (indexArr[i] == INVALID_INDEX)
                    continue;

                removeList.Add(indexArr[i]);
            }

            removeList.Sort();
            for (int i = removeList.Count - 1; i >= 0; --i)
                _entriesProperty.DeleteArrayElementAtIndex(removeList[i]);

            _tableObject.ApplyModifiedProperties();
            Undo.SetCurrentGroupName(REMOVE_KEY_UNDO_LABEL);
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

        static string GetFlowName(TutorialStepCatalogData.EFlow flow)
        {
            switch (flow)
            {
                case TutorialStepCatalogData.EFlow.Triggered:
                    return "행동 트리거 단계";
                case TutorialStepCatalogData.EFlow.PlayStageSelectIntro:
                    return "첫 스테이지 선택 인트로";
                case TutorialStepCatalogData.EFlow.PlayBriefing:
                    return "인게임 브리핑";
                case TutorialStepCatalogData.EFlow.Shop:
                    return "상점 안내";
                case TutorialStepCatalogData.EFlow.StageGimmick:
                    return "스테이지 기믹 안내";
                default:
                    return "미분류";
            }
        }

        static string GetFlowHint(TutorialStepCatalogData.EFlow flow)
        {
            if (TutorialStepCatalogData.IsOrderedFlow(flow))
                return ORDERED_HINT;

            return TRIGGERED_HINT;
        }

        static string GetTriggerNote(int stepIndex)
        {
            if (stepIndex < 0 || stepIndex >= STEP_VALUE_ARR.Length)
                return string.Empty;

            switch (STEP_VALUE_ARR[stepIndex])
            {
                case TutorialStepCatalogData.EStep.PlayLaunchGuide:
                    return "· 브리핑 직후 표시, 발사 입력까지 유지";
                case TutorialStepCatalogData.EStep.PlayFlipper:
                    return "· 볼이 플리퍼 접근 존에 들어오면";
                case TutorialStepCatalogData.EStep.PlayParry:
                    return "· 플리퍼로 볼을 쳐낸 직후 이어서";
                case TutorialStepCatalogData.EStep.PlayNudge:
                    return "· 슬링샷을 맞히면 (맵에 슬링샷이 없으면 즉시)";
                case TutorialStepCatalogData.EStep.PlayNudgeTry:
                    return "· 넛지 안내 직후, 넛지 입력까지 시간 정지";
                case TutorialStepCatalogData.EStep.PlayBallLost:
                    return "· 게임 중 처음 볼을 잃으면 (스테이지 무관), 재발사까지 유지";
                case TutorialStepCatalogData.EStep.PlayCombo:
                    return "· 콤보 조건 도달 또는 대기 시간 종료 시";
                case TutorialStepCatalogData.EStep.PlayFreePlay:
                    return "· 스테이지 클리어를 가로채지 않았을 때만, 마지막";
                default:
                    return string.Empty;
            }
        }

        static string GetStepName(int stepIndex)
        {
            if (stepIndex < 0 || stepIndex >= STEP_VALUE_ARR.Length)
                return string.Empty;

            return STEP_VALUE_ARR[stepIndex].ToString();
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
