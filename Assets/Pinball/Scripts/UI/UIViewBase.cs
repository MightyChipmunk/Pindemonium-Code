using GameCore.UI;

namespace Pinball.UI
{
    public abstract class UIViewBase : GameCore.UI.UIViewBase<UIViewCatalogData.EViewType>
    {
        public sealed override void Bind(GameCore.UI.UIViewArgs<UIViewCatalogData.EViewType> args)
        {
            Bind(args as UIViewArgs);
        }

        public abstract void Bind(UIViewArgs args);
    }
}
