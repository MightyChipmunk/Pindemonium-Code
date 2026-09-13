using System;
using System.Collections.Generic;
using Pinball.Ingame.Map;
using Pinball.UI;
using UnityEngine;
using GameCore.UI;

namespace Pinball.Ingame.Tutorial
{
    public class TutorialHighlighter
    {
        public record InitParam(
            Func<MapRoot> MapGetter,
            Func<RectTransform> ScoreAnchorGetter,
            Func<RectTransform> ComboAnchorGetter);

        const float WORLD_FOCUS_PADDING = 14f;

        static readonly Vector2 FALLBACK_FLIPPER_EXTENTS = new Vector2(1.1f, 0.6f);
        static readonly Vector2 FALLBACK_SPRING_EXTENTS = new Vector2(0.5f, 1.2f);
        static readonly Vector2 FALLBACK_GIMMICK_EXTENTS = new Vector2(0.6f, 0.6f);

        readonly InitParam _param;
        readonly TutorialDimmer _dimmer;
        readonly TutorialHighlightLayer _highlightLayer;
        readonly List<Bounds> _worldBoundsBuffer = new List<Bounds>();
        readonly List<MapInteractable.SpriteSnapshot> _spriteBuffer = new List<MapInteractable.SpriteSnapshot>();
        readonly List<MapInteractable.LineSnapshot> _lineBuffer = new List<MapInteractable.LineSnapshot>();
        readonly List<RectTransform> _anchorBuffer = new List<RectTransform>();

        public TutorialHighlighter(InitParam param, TutorialDimmer dimmer, TutorialHighlightLayer highlightLayer)
        {
            _param = param;
            _dimmer = dimmer;
            _highlightLayer = highlightLayer;
        }

        // Returns false when the target does not exist on the current map or view,
        // so a caller touring optional gimmicks can skip that step without having
        // disturbed the overlay that is already showing.
        public bool Apply(TutorialStepCatalogData.EHighlight highlight)
        {
            switch (highlight)
            {
                case TutorialStepCatalogData.EHighlight.None:
                    Clear();
                    return true;
                case TutorialStepCatalogData.EHighlight.FullScreen:
                    _highlightLayer.Clear();
                    _dimmer.ShowFull();
                    return true;
                case TutorialStepCatalogData.EHighlight.IngameScore:
                    return FocusUI(_param.ScoreAnchorGetter());
                case TutorialStepCatalogData.EHighlight.IngameCombo:
                    return FocusUI(_param.ComboAnchorGetter());
                case TutorialStepCatalogData.EHighlight.WorldSpring:
                    return FocusSpring();
                case TutorialStepCatalogData.EHighlight.WorldFlipper:
                    return FocusFlippers();
                case TutorialStepCatalogData.EHighlight.StageSelectCard:
                case TutorialStepCatalogData.EHighlight.StageSelectStart:
                case TutorialStepCatalogData.EHighlight.StageSelectGimmick:
                case TutorialStepCatalogData.EHighlight.StageSelectReroll:
                    return FocusStageSelect(highlight);
                case TutorialStepCatalogData.EHighlight.ShopPacks:
                    return FocusShopPacks();
                case TutorialStepCatalogData.EHighlight.ShopOffers:
                case TutorialStepCatalogData.EHighlight.ShopVoucher:
                case TutorialStepCatalogData.EHighlight.ShopReroll:
                case TutorialStepCatalogData.EHighlight.ShopMoney:
                case TutorialStepCatalogData.EHighlight.ShopStart:
                    return FocusShop(highlight);
                default:
                    return FocusGimmick(ToGimmickType(highlight));
            }
        }

        public void Clear()
        {
            _highlightLayer.Clear();
            _dimmer.Hide();
        }

        bool FocusUI(RectTransform target)
        {
            if (null == target)
                return false;

            _dimmer.ShowFull();
            _highlightLayer.ShowUITarget(target);
            return true;
        }

        bool FocusStageSelect(TutorialStepCatalogData.EHighlight highlight)
        {
            StageSelectView view = UIManager.GetActive(UIViewCatalogData.EViewType.StageSelect) as StageSelectView;
            if (null == view)
                return false;

            switch (highlight)
            {
                case TutorialStepCatalogData.EHighlight.StageSelectCard:
                    return FocusUI(view.GetCurrentCardAnchor());
                case TutorialStepCatalogData.EHighlight.StageSelectGimmick:
                    return FocusUI(view.GetCurrentGimmickAnchor());
                case TutorialStepCatalogData.EHighlight.StageSelectReroll:
                    return FocusUI(view.GetCurrentRerollAnchor());
                default:
                    return FocusUI(view.GetCurrentStartAnchor());
            }
        }

        bool FocusShop(TutorialStepCatalogData.EHighlight highlight)
        {
            ShopView view = UIManager.GetActive(UIViewCatalogData.EViewType.Shop) as ShopView;
            if (null == view)
                return false;

            switch (highlight)
            {
                case TutorialStepCatalogData.EHighlight.ShopOffers:
                    return FocusUI(view.GetOfferRowAnchor());
                case TutorialStepCatalogData.EHighlight.ShopVoucher:
                    return FocusUI(view.GetVoucherAnchor());
                case TutorialStepCatalogData.EHighlight.ShopReroll:
                    return FocusUI(view.GetRerollAnchor());
                case TutorialStepCatalogData.EHighlight.ShopMoney:
                    return FocusUI(view.GetMoneyAnchor());
                default:
                    return FocusUI(view.GetStartAnchor());
            }
        }

        bool FocusShopPacks()
        {
            ShopView view = UIManager.GetActive(UIViewCatalogData.EViewType.Shop) as ShopView;
            if (null == view)
                return false;

            _anchorBuffer.Clear();
            for (int i = 0, count = view.GetPackSlotCount(); i < count; ++i)
                _anchorBuffer.Add(view.GetPackSlotAnchor(i));

            if (_anchorBuffer.Count <= 0)
                return false;

            _dimmer.ShowFull();
            _highlightLayer.ShowUITargets(_anchorBuffer);
            return true;
        }

        bool FocusSpring()
        {
            MapRoot map = _param.MapGetter();
            if (null == map)
                return false;

            _worldBoundsBuffer.Clear();
            if (map.TryGetSpringBounds(out Bounds springBounds))
                _worldBoundsBuffer.Add(springBounds);
            else
                _worldBoundsBuffer.Add(CreateFallbackBounds(map.GetSpringPosition(), FALLBACK_SPRING_EXTENTS));

            _spriteBuffer.Clear();
            _lineBuffer.Clear();
            map.CollectSpringSprites(_spriteBuffer);
            FocusWorldTargets();
            return true;
        }

        bool FocusFlippers()
        {
            MapRoot map = _param.MapGetter();
            if (null == map)
                return false;

            _worldBoundsBuffer.Clear();
            _worldBoundsBuffer.Add(GetFlipperBounds(map, true));
            _worldBoundsBuffer.Add(GetFlipperBounds(map, false));
            _spriteBuffer.Clear();
            _lineBuffer.Clear();
            map.CollectLeftFlipperSprites(_spriteBuffer);
            map.CollectRightFlipperSprites(_spriteBuffer);
            FocusWorldTargets();
            return true;
        }

        bool FocusGimmick(MapInteractable.EGimmickType gimmickType)
        {
            MapRoot map = _param.MapGetter();
            if (null == map)
                return false;

            _worldBoundsBuffer.Clear();
            _spriteBuffer.Clear();
            _lineBuffer.Clear();
            for (int i = 0, count = map.GetInteractableCount(); i < count; ++i)
            {
                MapInteractable interactable = map.GetInteractable(i);
                if (null == interactable || interactable.GimmickType != gimmickType)
                    continue;

                if (interactable.IsDeactivated)
                    continue;

                if (gimmickType == MapInteractable.EGimmickType.Lane)
                    interactable.CollectHighlightLines(_lineBuffer);
                else
                    interactable.CollectHighlightSprites(_spriteBuffer);

                if (interactable.TryGetWorldBounds(out Bounds worldBounds))
                    _worldBoundsBuffer.Add(worldBounds);
                else
                    _worldBoundsBuffer.Add(CreateFallbackBounds(interactable.transform.position, FALLBACK_GIMMICK_EXTENTS));
            }

            if (_worldBoundsBuffer.Count <= 0 && _spriteBuffer.Count <= 0)
                return false;

            FocusWorldTargets();
            return true;
        }

        void FocusWorldTargets()
        {
            if (_spriteBuffer.Count > 0 || _lineBuffer.Count > 0)
            {
                _dimmer.ShowFull();
                _highlightLayer.ShowWorldTargets(_spriteBuffer, _lineBuffer);
                return;
            }

            _highlightLayer.Clear();
            _dimmer.FocusWorldBounds(_worldBoundsBuffer, WORLD_FOCUS_PADDING);
        }

        static Bounds GetFlipperBounds(MapRoot map, bool isLeft)
        {
            if (isLeft && map.TryGetLeftFlipperBounds(out Bounds leftBounds))
                return leftBounds;

            if (!isLeft && map.TryGetRightFlipperBounds(out Bounds rightBounds))
                return rightBounds;

            Vector2 fallbackCenter = isLeft ? map.GetLeftFlipperPosition() : map.GetRightFlipperPosition();
            return CreateFallbackBounds(fallbackCenter, FALLBACK_FLIPPER_EXTENTS);
        }

        static Bounds CreateFallbackBounds(Vector2 worldCenter, Vector2 worldExtents)
        {
            return new Bounds(worldCenter, worldExtents * 2f);
        }

        static MapInteractable.EGimmickType ToGimmickType(TutorialStepCatalogData.EHighlight highlight)
        {
            switch (highlight)
            {
                case TutorialStepCatalogData.EHighlight.WorldBumper:
                    return MapInteractable.EGimmickType.Bumper;
                case TutorialStepCatalogData.EHighlight.WorldSlingshot:
                    return MapInteractable.EGimmickType.Slingshot;
                case TutorialStepCatalogData.EHighlight.WorldSpinner:
                    return MapInteractable.EGimmickType.Spinner;
                case TutorialStepCatalogData.EHighlight.WorldDropTarget:
                    return MapInteractable.EGimmickType.DropTarget;
                case TutorialStepCatalogData.EHighlight.WorldStandupTarget:
                    return MapInteractable.EGimmickType.StandupTarget;
                case TutorialStepCatalogData.EHighlight.WorldLampBank:
                    return MapInteractable.EGimmickType.LampBank;
                case TutorialStepCatalogData.EHighlight.WorldLane:
                    return MapInteractable.EGimmickType.Lane;
                default:
                    return MapInteractable.EGimmickType.Saucer;
            }
        }
    }
}
