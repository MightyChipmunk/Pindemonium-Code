using Pinball.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class GimmickScoreView : UIViewBase
    {
        public record Entry(LocalizedTextRef NameRef, long Score);

        public record Args(Entry[] Entries) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.GimmickScore;
        }

        const int TOP_ENTRY_INDEX = 0;
        const float EMPTY_RATIO = 0f;

        [SerializeField] GimmickScoreRowView[] _rows = null;
        [SerializeField] Button _closeButton = null;

        public override void OnGenerate()
        {
            _closeButton.onClick.AddListener(OnCloseClicked);
        }

        public override void OnTerminate()
        {
            _closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        public override bool TryCloseByEscape()
        {
            OnCloseClicked();
            return true;
        }

        public override void Bind(UIViewArgs args)
        {
            Args viewArgs = args as Args;
            if (null == viewArgs)
                return;

            BindRows(viewArgs.Entries);
        }

        void BindRows(Entry[] entries)
        {
            int entryCount = null == entries ? 0 : entries.Length;
            long maxScore = GetMaxScore(entries);
            for (int i = 0; i < _rows.Length; ++i)
            {
                if (i >= entryCount || null == entries[i])
                {
                    _rows[i].SetVisible(false);
                    continue;
                }

                Entry entry = entries[i];
                _rows[i].SetVisible(true);
                _rows[i].Bind(entry.NameRef, entry.Score, GetRatio(entry.Score, maxScore), i == TOP_ENTRY_INDEX && entry.Score > 0);
            }
        }

        static long GetMaxScore(Entry[] entries)
        {
            long maxScore = 0;
            int entryCount = null == entries ? 0 : entries.Length;
            for (int i = 0; i < entryCount; ++i)
            {
                if (null == entries[i])
                    continue;

                if (entries[i].Score > maxScore)
                    maxScore = entries[i].Score;
            }

            return maxScore;
        }

        static float GetRatio(long score, long maxScore)
        {
            if (maxScore <= 0)
                return EMPTY_RATIO;

            return (float)score / maxScore;
        }

        void OnCloseClicked()
        {
            UIManager.Hide(UIViewCatalogData.EViewType.GimmickScore);
        }
    }
}
