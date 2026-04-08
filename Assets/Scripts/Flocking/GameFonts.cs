using UnityEngine;

// loads the shared fonts used by the game's ui and labels
public static class GameFonts
{
    // -------------------------------------------------------------------------------------------------------------

    private static Font regular;
    private static Font bold;

    public static Font Regular => regular != null ? regular : LoadRegular();
    public static Font Bold => bold != null ? bold : LoadBold();

    // loads the regular silkscreen font from resources
    private static Font LoadRegular()
    {
        regular = Resources.Load<Font>("Fonts/Silkscreen-Regular");
        return regular;
    }

    // loads the bold silkscreen font from resources
    private static Font LoadBold()
    {
        bold = Resources.Load<Font>("Fonts/Silkscreen-Bold");
        return bold;
    }
}
