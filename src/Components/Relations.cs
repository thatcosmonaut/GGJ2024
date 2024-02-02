using MoonTools.ECS;

namespace RollAndCash.Relations;

public readonly record struct Colliding();
public readonly record struct Holding();
public readonly record struct IsInCategory();
public readonly record struct HasIngredient();
public readonly record struct RequiresCategory();
public readonly record struct RequiresIngredient();
public readonly record struct Inspecting();
public readonly record struct ShowingPopup();
public readonly record struct DisplayingProductPrice();
public readonly record struct DisplayingIngredientPrice();
public readonly record struct HasScore();
public readonly record struct UpdateDisplayScoreOnDestroy();
public readonly record struct TimingFootstepAudio();
public readonly record struct BelongsToProductSpawner();
public readonly record struct ConsideredProduct();
public readonly record struct CantLeaveStore();
public readonly record struct CantTalk();
public readonly record struct OrderTimer();
public readonly record struct OrderPriceText();
public readonly record struct OrderIcon();
public readonly record struct HoldingText();
public readonly record struct TeleportToAtTimerEnd(Entity TeleportTo);
public readonly record struct Targeting();
