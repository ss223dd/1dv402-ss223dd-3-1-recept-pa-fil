using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FiledRecipes.Domain
{ 
    /// <summary>
    /// Holder for recipes.
    /// </summary>
    public class RecipeRepository : IRecipeRepository
    {
        /// <summary>
        /// Represents the recipe section.
        /// </summary>
        private const string SectionRecipe = "[Recept]";

        /// <summary>
        /// Represents the ingredients section.
        /// </summary>
        private const string SectionIngredients = "[Ingredienser]";

        /// <summary>
        /// Represents the instructions section.
        /// </summary>
        private const string SectionInstructions = "[Instruktioner]";

        /// <summary>
        /// Occurs after changes to the underlying collection of recipes.
        /// </summary>
        public event EventHandler RecipesChangedEvent;

        /// <summary>
        /// Specifies how the next line read from the file will be interpreted.
        /// </summary>
        private enum RecipeReadStatus { Indefinite, New, Ingredient, Instruction };

        /// <summary>
        /// Collection of recipes.
        /// </summary>
        private List<IRecipe> _recipes;

        /// <summary>
        /// The fully qualified path and name of the file with recipes.
        /// </summary>
        private string _path;

        /// <summary>
        /// Indicates whether the collection of recipes has been modified since it was last saved.
        /// </summary>
        public bool IsModified { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the RecipeRepository class.
        /// </summary>
        /// <param name="path">The path and name of the file with recipes.</param>
        public RecipeRepository(string path)
        {
            // Throws an exception if the path is invalid.
            _path = Path.GetFullPath(path);

            _recipes = new List<IRecipe>();
        }

        /// <summary>
        /// Returns a collection of recipes.
        /// </summary>
        /// <returns>A IEnumerable&lt;Recipe&gt; containing all the recipes.</returns>
        public virtual IEnumerable<IRecipe> GetAll()
        {
            // Deep copy the objects to avoid privacy leaks.
            return _recipes.Select(r => (IRecipe)r.Clone());
        }

        /// <summary>
        /// Returns a recipe.
        /// </summary>
        /// <param name="index">The zero-based index of the recipe to get.</param>
        /// <returns>The recipe at the specified index.</returns>
        public virtual IRecipe GetAt(int index)
        {
            // Deep copy the object to avoid privacy leak.
            return (IRecipe)_recipes[index].Clone();
        }

        /// <summary>
        /// Deletes a recipe.
        /// </summary>
        /// <param name="recipe">The recipe to delete. The value can be null.</param>
        public virtual void Delete(IRecipe recipe)
        {
            // If it's a copy of a recipe...
            if (!_recipes.Contains(recipe))
            {
                // ...try to find the original!
                recipe = _recipes.Find(r => r.Equals(recipe));
            }
            _recipes.Remove(recipe);
            IsModified = true;
            OnRecipesChanged(EventArgs.Empty);
        }

        /// <summary>
        /// Deletes a recipe.
        /// </summary>
        /// <param name="index">The zero-based index of the recipe to delete.</param>
        public virtual void Delete(int index)
        {
            Delete(_recipes[index]);
        }

        /// <summary>
        /// Raises the RecipesChanged event.
        /// </summary>
        /// <param name="e">The EventArgs that contains the event data.</param>
        protected virtual void OnRecipesChanged(EventArgs e)
        {
            // Make a temporary copy of the event to avoid possibility of 
            // a race condition if the last subscriber unsubscribes 
            // immediately after the null check and before the event is raised.
            EventHandler handler = RecipesChangedEvent;

            // Event will be null if there are no subscribers. 
            if (handler != null)
            {
                // Use the () operator to raise the event.
                handler(this, e);
            }
        }
        /// THE LOAD METHOD - "1. ÖPPNA":
        /// If user opts 1 - the method should open/load the text-file "recipes.txt", then read and parse each line in order to create a <List> of recipes 
        /// which leaves the user with further options in ways of managing the list.
        /// 
        /// (Recipe format:
        /// Varje recept består av avdelningarna [Recept], [Ingredienser] och [Instruktioner]. Raden som 
        /// följer efter [Recept] är receptets namn. Raderna som följer efter [Ingredienser], fram till 
        /// [Instruktioner] är receptets ingredienser där varje rad innehåller en ingrediens. Raderna som följer 
        /// [Instruktioner], fram till [Recept] eller slutet på filen, är receptets instruktioner där varje rad är en 
        /// instruktion.)
        /// 
        /// Algorithm of .Load() which creates a list with references to Recipe-objects:
        /// 
        /// 1. Create List which can contain ref's to rec-obj's
        /// 2. Open .txt for reading.
        /// 3. Read line from .txt til end of file.
        ///     a. If empty line - continue to read next line.
        ///     b. If section of new recipe [Recept] - next line is recipe name.
        ///     c. If section of [Ingredienser] - set status to "following lines are ingredients".
        ///     d. Section of [Instruktioner] - status = following lines are instructions.
        ///     e. Else - a name, an ingredient, an instruction.
        ///         i. - if status = name - create new recipe-obj with the name.
        ///         ii. - status = ingredient - split the line using the method Split() in the class String. Should always be in 3 parts due to the ";".
        ///                                   - if amount of parts != 3 - throw FileFormatException.
        ///                                   - Create an ingredient-obj and initiate with the 3 parts of amount, unit and name.
        ///                                   - Add the ingredient to the recipe's list of ingredients.
        ///         iii. - status = instruction - Add line to recipe's list of instructions. 
        ///         iv. - else... something is wrong = throw FileFormatException.
        /// 4. Sort list of recipes based on name.
        /// 5. Assign relevant field in class "_recipes" with a reference to the list.
        /// 6. Assign relevant property in the class "IsModified" a value indicating that the list of recipes is unchanged.
        /// 7. Call method "OnRecipesChanged" with parameter (EventArgs.Empty) to let the app know recipes have been loaded/read.

        public void Load()
        {
            List<IRecipe> recipeList = new List<IRecipe>();

            RecipeReadStatus readStatus = RecipeReadStatus.Indefinite;

            try
            {
                using (StreamReader reader = new StreamReader(_path))
                {
                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }
                        switch (line)
                        {
                            case SectionRecipe:
                                readStatus = RecipeReadStatus.New;
                                break;

                            case SectionIngredients:
                                readStatus = RecipeReadStatus.Ingredient;
                                break;

                            case SectionInstructions:
                                readStatus = RecipeReadStatus.Instruction;
                                break;
                        }
                        switch (readStatus)
                        {
                            case RecipeReadStatus.New:
                                recipeList.Add(new Recipe(line));
                                    break;

                            case RecipeReadStatus.Ingredient:
                                string[] splitParts = line.Split(';');
                                    
                                    
                                    
                                    /// ii. - status = ingredient - split the line using the method Split() in the class String. Should always be in 3 parts due to the ";".
                                    ///                           - if amount of parts != 3 - throw FileFormatException.
                                    ///                           - Create an ingredient-obj and initiate with the 3 parts of amount, unit and name.
                                    ///                           - Add the ingredient to the recipe's list of ingredients.
                                if(splitParts.Length != 3)
                                {
                                    throw new FileFormatException();
                                }
                                
                                

                                break;

                            case RecipeReadStatus.Instruction:
                                // iii. - status = instruction - Add line to recipe's list of instructions.
                                
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Oväntat fel inträffade!\n {0}", ex.Message);
            }
        }

        /// THE SAVE METHOD - "2. SPARA":
        /// User opts 2 - recipes should be permanently saved in the text file by first opening the file then writing each recipe, line by line. 
        /// If the file already exists it should be replaced/overwritten.  
        /// 

        public void Save()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(""))
                {
                    writer.WriteLine("");
                    writer.WriteLine("");
                    writer.WriteLine("");

                    writer.Write(writer.NewLine);
                    writer.WriteLine();
                }

                Console.WriteLine("Skapade en fil med några rader.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Oväntat fel inträffade!\n {0}", ex.Message);
            }
        }
    }
}
