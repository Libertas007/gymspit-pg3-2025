namespace Lecture19;

// AI characters
public class Berserker() 
    : AICharacter(0.95, "Berserker", 22, 4, 1, 0.75, 0.30, "red");

public class Guardian() 
    : AICharacter(0.40, "Guardian", 24, 2, 3, 0.80, 0.10, "blue");

public class Assassin() 
    : AICharacter(0.85, "Assassin", 18, 4, 1, 0.85, 0.40, "black");

public class Sniper() 
    : AICharacter(0.70, "Sniper", 17, 4, 1, 0.95, 0.25, "darkgreen");

public class Paladin() 
    : AICharacter(0.55, "Paladin", 21, 3, 2, 0.85, 0.15, "yellow");

public class Warlock() 
    : AICharacter(0.75, "Warlock", 19, 3, 1, 0.80, 0.20, "purple");

public class Monk() 
    : AICharacter(0.60, "Monk", 20, 2, 2, 0.90, 0.18, "orange1");

public class Necromancer() 
    : AICharacter(0.65, "Necromancer", 20, 3, 2, 0.78, 0.22, "darkmagenta");

public class Knight() 
    : AICharacter(0.50, "Knight", 23, 3, 3, 0.82, 0.12, "silver");

public class Rogue() 
    : AICharacter(0.80, "Rogue", 18, 3, 1, 0.88, 0.28, "gray");

public class Elementalist() 
    : AICharacter(0.70, "Elementalist", 19, 3, 1, 0.83, 0.24, "cyan");

public class Warlord() 
    : AICharacter(0.85, "Warlord", 24, 4, 2, 0.80, 0.20, "darkred");


// Player characters
public class Dragoon() 
    : PlayerCharacter("Dragoon", 21, 3, 2, 0.88, 0.22, "steelblue");

public class Spellblade() 
    : PlayerCharacter("Spellblade", 20, 3, 2, 0.90, 0.24, "purple4");

public class Beastmaster() 
    : PlayerCharacter("Beastmaster", 22, 2, 3, 0.85, 0.18, "rosybrown");

public class Chronomancer() 
    : PlayerCharacter("Chronomancer", 17, 4, 1, 0.92, 0.26, "teal");

public class Sentinel() 
    : PlayerCharacter("Sentinel", 24, 2, 3, 0.82, 0.14, "navy");

public class Stormcaller() 
    : PlayerCharacter("Stormcaller", 18, 4, 1, 0.89, 0.23, "aqua");

public class Blademaster() 
    : PlayerCharacter("Blademaster", 22, 3, 2, 0.91, 0.25, "indianred");

public class Templar() 
    : PlayerCharacter("Templar", 23, 2, 3, 0.86, 0.17, "yellow4");

public class Gunslinger() 
    : PlayerCharacter("Gunslinger", 17, 4, 1, 0.94, 0.28, "darkslategray1");

public class Arcanist() 
    : PlayerCharacter("Arcanist", 16, 4, 1, 0.87, 0.29, "mediumorchid");

public class Warden() 
    : PlayerCharacter("Warden", 24, 2, 3, 0.80, 0.12, "darkolivegreen1");

public class Illusionist() 
    : PlayerCharacter("Illusionist", 18, 3, 2, 0.95, 0.30, "plum1");