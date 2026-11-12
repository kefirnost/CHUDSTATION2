using System.Linq;
using Content.Client.Lobby.UI.Roles;
using Content.Client.Stylesheets;
using Content.Shared.CCVar;
using Content.Shared.Traits;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    /// <summary>
    /// Refreshes traits selector.
    /// </summary>
    public void RefreshTraits()
    {
        TraitsList.RemoveAllChildren();

        var traits = _prototypeManager.EnumeratePrototypes<TraitPrototype>()
            .OrderBy(t => Loc.GetString(t.Name))
            .ToList();
        TabContainer.SetTabTitle(3, Loc.GetString("humanoid-profile-editor-traits-tab"));

        if (traits.Count < 1)
        {
            TraitsList.AddChild(new Label
            {
                Text = Loc.GetString("humanoid-profile-editor-no-traits"),
                FontColorOverride = Color.Gray,
            });
            return;
        }

        var maxTraitPoints = Math.Max(0, _cfgManager.GetCVar(CCVars.TraitPointsBudget));
        var selectedTraitPoints = 0;

        if (Profile != null)
        {
            foreach (var traitId in Profile.TraitPreferences)
            {
                if (_prototypeManager.TryIndex<TraitPrototype>(traitId, out var selectedTrait))
                    selectedTraitPoints += selectedTrait.Cost;
            }
        }

        TraitsList.AddChild(new Label
        {
            Text = Loc.GetString(
                "humanoid-profile-editor-trait-points-available",
                ("points", maxTraitPoints - selectedTraitPoints)),
            FontColorOverride = Color.Gray,
        });

        // Setup model.
        Dictionary<string, List<string>> traitGroups = new();
        List<string> defaultTraits = new();
        traitGroups.Add(TraitCategoryPrototype.Default, defaultTraits);

        foreach (var trait in traits)
        {
            if (trait.Category == null)
            {
                defaultTraits.Add(trait.ID);
                continue;
            }

            if (!_prototypeManager.HasIndex(trait.Category))
                continue;

            var group = traitGroups.GetOrNew(trait.Category);
            group.Add(trait.ID);
        }

        // Create UI view from model.
        foreach (var (categoryId, categoryTraits) in traitGroups)
        {
            TraitCategoryPrototype? category = null;

            if (categoryId != TraitCategoryPrototype.Default)
            {
                category = _prototypeManager.Index<TraitCategoryPrototype>(categoryId);
                TraitsList.AddChild(new Label
                {
                    Text = Loc.GetString(category.Name),
                    Margin = new Thickness(0, 10, 0, 0),
                    StyleClasses = { StyleClass.LabelHeading },
                });
            }

            List<TraitPreferenceSelector> selectors = new();
            var selectedCategoryPoints = 0;
            var selectedCategoryCount = 0;

            foreach (var traitProto in categoryTraits)
            {
                var trait = _prototypeManager.Index<TraitPrototype>(traitProto);
                var selector = new TraitPreferenceSelector(trait);
                selector.Preference = Profile?.TraitPreferences.Contains(trait.ID) == true;

                if (selector.Preference)
                {
                    selectedCategoryPoints += trait.Cost;
                    selectedCategoryCount++;
                }

                selector.PreferenceChanged += preference =>
                {
                    if (preference)
                    {
                        Profile = Profile?.WithTraitPreference(trait.ID, _prototypeManager, maxTraitPoints);
                    }
                    else
                    {
                        Profile = Profile?.WithoutTraitPreference(trait.ID, _prototypeManager);
                    }

                    SetDirty();
                    RefreshTraits();
                };

                selectors.Add(selector);
            }

            if (category is { MaxTraitPoints: >= 0 })
            {
                TraitsList.AddChild(new Label
                {
                    Text = Loc.GetString(
                        "humanoid-profile-editor-trait-count-hint",
                        ("current", selectedCategoryPoints),
                        ("max", category.MaxTraitPoints)),
                    FontColorOverride = Color.Gray,
                });
            }

            if (category is { MaxTraits: >= 0 })
            {
                TraitsList.AddChild(new Label
                {
                    Text = Loc.GetString(
                        "humanoid-profile-editor-trait-category-count-hint",
                        ("current", selectedCategoryCount),
                        ("max", category.MaxTraits)),
                    FontColorOverride = Color.Gray,
                });
            }

            foreach (var selector in selectors)
            {
                if (!selector.Preference)
                {
                    var exceedsGlobalBudget = selectedTraitPoints + selector.Cost > maxTraitPoints;
                    var exceedsCategoryPoints = category is { MaxTraitPoints: >= 0 } &&
                                                selectedCategoryPoints + selector.Cost > category.MaxTraitPoints.Value;
                    var exceedsCategoryCount = category is { MaxTraits: >= 0 } &&
                                               selectedCategoryCount >= category.MaxTraits.Value;

                    if (exceedsGlobalBudget || exceedsCategoryPoints || exceedsCategoryCount)
                    {
                        selector.Checkbox.Disabled = true;
                        selector.Checkbox.Label.FontColorOverride = Color.Red;
                    }
                }

                TraitsList.AddChild(selector);
            }
        }
    }
}
