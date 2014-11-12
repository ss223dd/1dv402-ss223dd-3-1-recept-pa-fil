using FiledRecipes.Domain;
using FiledRecipes.App.Mvp;
using FiledRecipes.Properties;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FiledRecipes.Views
{
    /// <summary>
    /// Add the other two methods showRecipe and showAllRecipes in this class.
    /// </summary>
    public class RecipeView : ViewBase, IRecipeView
    {
      
    }
    /// THE VIEW RECIPE METHOD - "4. VISA RECEPT"
    /// User opts 4 - a list with the name of all recipes should be shown from which the user opts which one to present in full detail.



    /// THE VIEW ALL RECIPES METHOD - "5. VISA ALLA RECEPT"
    /// User opts 5 - all recipes is shown ordered by name. However each recipe is only shown one by one, one at a time 
    /// and user needs to press a key to show the next one. After all has been presented, the user should press a key to return to the main menu.
}
