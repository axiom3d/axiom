using System;

namespace Axiom.SceneManagers.Octree {
    /// <summary>
    ///     Direction of a tile in reference to another tile.
    /// </summary>
    [Flags]
    public enum Tile {
        North = 1,
        South = 2,
        West = 4,
        East = 8
    }

    /// <summary>
    /// 
    /// </summary>
    public enum Neighbor {
        North,
        South,
        East,
        West,
        Here
    }
}
