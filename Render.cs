using SFML.Graphics;
using SFML.System;
using System.Linq;
using System.Threading.Tasks;

public class Tile
{
    public string id {get; set;}
    public bool isActive {get; set;}
    public Color color {get; set;}
    public Color unactiveColor {get; set;}
}

public class TileRenderer
{
    private Tile[,] tiles;
    public Tile[,] Tiles => tiles;
    public RenderTexture mainSheet;
    public Vector2f position;
    public float Scale = 3f;
    public Cell[,] cellMap;

    public TileRenderer(int width, int height)
    {
        tiles = new Tile[width, height];
        mainSheet = new RenderTexture(new Vector2u((uint)width, (uint)height));
    }

    private Image pendingImage = null;
    private readonly object pendingLock = new object();

    public void SetTile(int x, int y, object tile)
    {
        if (tile == null)
        {
            if (cellMap == null) tiles[x, y] = null;
            else cellMap[x, y] = null;
            return;
        }

        if(!(tile is Cell) && !(tile is Tile)) return;

        if(cellMap == null) tiles[x, y] = (Tile)tile;
        else cellMap[x, y] = (Cell)tile;
    }

    public void UpdateMap()
    {
        Image img = null;
        lock (pendingLock)
        {
            img = pendingImage;
            pendingImage = null;
        }

        if (img != null)
        {
            mainSheet.Clear(Color.White);
            mainSheet.Texture.Update(img);
            mainSheet.Display();
            return;
        }

        mainSheet.Clear(Color.White);
        RectangleShape rect = new RectangleShape(new Vector2f(1, 1));
        for (int x = 0; x < tiles.GetLength(0); x++)
        {
            for (int y = 0; y < tiles.GetLength(1); y++)
            {
                Tile tile = cellMap == null ? tiles[x, y] : cellMap[x, y]?.tile;
                bool isActive = cellMap != null && cellMap[x, y] != null ?
                    cellMap[x, y].isActive : tiles[x, y]?.isActive ?? false;
                Color color = tile != null ? (isActive ? tile.color : tile.unactiveColor) : Color.White;
                if(cellMap != null && cellMap[x, y] != null && cellMap[x, y].isSource)
                {
                    if(cellMap[x, y].isActive)
                    {
                        color = new Color(
                            (byte)Math.Min(color.R + 100, 255),
                            (byte)Math.Min(color.G + 100, 255),
                            (byte)Math.Min(color.B + 100, 255)
                        );
                    }
                    else
                    {
                        color = new Color(
                            (byte)Math.Max(color.R - 40, 0),
                            (byte)Math.Max(color.G - 40, 0),
                            (byte)Math.Max(color.B - 40, 0)
                        );
                    }
                }

                rect.Position = new Vector2f(x, y);
                rect.FillColor = color;
                mainSheet.Draw(rect);
            }
        }

        mainSheet.Display();
    }

    public Task PrepareMapAsync()
    {
        return Task.Run(() =>
        {
            int width = tiles.GetLength(0);
            int height = tiles.GetLength(1);
            Image img = new Image(new Vector2u((uint)width, (uint)height), Color.White);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Tile tile = cellMap == null ? tiles[x, y] : cellMap[x, y]?.tile;
                    bool isActive = cellMap != null && cellMap[x, y] != null ?
                        cellMap[x, y].isActive : tiles[x, y]?.isActive ?? false;
                    Color color = tile != null ? (isActive ? tile.color : tile.unactiveColor) : Color.White;
                    Console.WriteLine($"{cellMap != null}{cellMap[x, y] != null}{cellMap[x, y] != null && cellMap[x, y].isSource}");

                    img.SetPixel(new Vector2u((uint)x, (uint)y), color);
                }
            }

            lock (pendingLock)
            {
                pendingImage = img;
            }
        });
    }

    public void Render(RenderWindow window)
    {
        Sprite sprite = new Sprite(mainSheet.Texture);
        sprite.Scale = new Vector2f(Scale * 10, Scale * 10);
        sprite.Position = position;
        window.Draw(sprite);
    }
}