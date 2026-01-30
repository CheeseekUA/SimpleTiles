using SFML.Graphics;
using SFML.Window;
using SFML.System;

/*
Simple Tiles
By: Cheeseek (Hlib Shvets)
*/

public class Cell
{
    public Tile tile;
    public bool isActive = false, isBlocked = false;
    public bool isSource = false, isConnectedToSource = false, isUniversal = false;
    public string[] disabled = new string[0];
    public string[] allow = new string[0];
}

public class Game
{
    public static Cell[,] cellMap;
    public static RenderWindow mainWindow;
    public static TileRenderer mainMap;
    public static Tile[] tileList = new Tile[]
    {
        new Tile{id="clear", color=new Color(100, 0, 0, 100)},
        new Tile{id="universal", color=new Color(50, 50, 50), unactiveColor=new Color(20, 20, 20)},
        new Tile{id="red", color=Color.Red, unactiveColor=new Color(128, 0, 0)},
        new Tile{id="green", color=Color.Green, unactiveColor=new Color(0, 128, 0)},
        new Tile{id="blue", color=Color.Blue, unactiveColor=new Color(0, 0, 128)},
        new Tile{id="yellow", color=Color.Yellow, unactiveColor=new Color(128, 128, 0)}
    };

    private static bool updateMap = false;
    private static bool workingOnUpdate = false;

    public static View gameView;
    public static View uiView;
    public static System.Timers.Timer updateTimer = new System.Timers.Timer(1000.0 / 2);

    public static Font mainFont;
    public static RenderTexture palleteCanvas, tileEditor;
    private static int selectedTile = 0;
    private static Vector2u currentChanging;
    private static bool inEditor = false;
    private static string[] currentAllow = new string[0];
    private static string[] currentDisable = new string[0];

    public static void Main()
    {
        string exeDir = AppContext.BaseDirectory;
        InitializeGame();

        palleteCanvas = new RenderTexture(new Vector2u(100, 500));
        palleteCanvas.Clear(Color.Transparent);

        tileEditor = new RenderTexture(new Vector2u(600, 500));

        RectangleShape infoBox = new RectangleShape(new Vector2f(300, 100));
        infoBox.FillColor = new Color(0, 0, 0, 150);
        infoBox.Position = new Vector2f(10, 10);
        infoBox.OutlineColor = Color.Black;
        infoBox.OutlineThickness = 2f;

        UpdatePallete();

        mainFont = new Font(Path.Combine(exeDir, "Assets", "mainFont.ttf"));

        UpdateEditor();

        Text infoText = new Text(mainFont);
        infoText.CharacterSize = 14;
        infoText.FillColor = Color.White;
        infoText.Position = new Vector2f(20, 20);

        updateTimer.Elapsed += async (sender, e) =>
        {
            if(!workingOnUpdate)
            {
                await UpdateCellmap();
            }
        };
        updateTimer.AutoReset = true;
        updateTimer.Start();

        while(mainWindow.IsOpen)
        {
            mainWindow.DispatchEvents();

            if(updateMap)
            {
                mainMap.UpdateMap();
            }

            var current = GetCurrentTile();
            string id = current?.tile?.id ?? "null";
            string isActive = current?.isActive.ToString() ?? "null";
            string isSource = current?.isSource.ToString() ?? "null";
            if(current != null && !current.isSource && current.isConnectedToSource)
            {
                isSource = "Powered";
            }

            infoText.DisplayedString = $"Current Tile Info:\n" +
                                    $"Mouse At {Mouse.GetPosition(mainWindow).X}, {Mouse.GetPosition(mainWindow).Y}\n" +
                                    $"IsActive: {isActive}; Is Source: {isSource}\n" +
                                    $"ID: {id}\n" +
                                    $"Working On Update: {workingOnUpdate}\n";

            mainWindow.Clear(Color.Black);

            mainWindow.SetView(gameView);
            mainMap.Render(mainWindow);

            mainWindow.SetView(uiView);
            mainWindow.Draw(infoBox);
            mainWindow.Draw(infoText);
            Sprite palleteSprite = new Sprite(palleteCanvas.Texture);
            palleteSprite.Position = new Vector2f(10, 50);
            mainWindow.Draw(palleteSprite);
            if(inEditor)
            {
                Sprite tileEditorSprite = new Sprite(tileEditor.Texture);
                tileEditorSprite.Scale = new Vector2f(1f, -1f);
                tileEditorSprite.Position = new Vector2f(mainWindow.Size.X / 2 - 300,
                mainWindow.Size.Y / 2 + 250);
                mainWindow.Draw(tileEditorSprite);
            }

            mainWindow.Display();
        }
    }

    public static void UpdateEditor()
    {
        tileEditor.Clear(Color.Transparent);
        RectangleShape popup = new RectangleShape(new Vector2f(550, 450));
        popup.FillColor = new Color(0, 0, 0, 150);
        popup.Position = new Vector2f(40, 40);
        popup.OutlineColor = Color.Black;
        popup.OutlineThickness = 2f;
        tileEditor.Draw(popup);

        Text title = new Text(mainFont);
        title.CharacterSize = 25;
        title.FillColor = Color.White;
        title.Position = new Vector2f(50, 50);
        title.DisplayedString = "Tile Editor";
        tileEditor.Draw(title);

        Text isSource = new Text(mainFont);
        isSource.CharacterSize = 18;
        isSource.FillColor = Color.White;
        isSource.Position = new Vector2f(50, 145);
        isSource.DisplayedString = $"Is Source: {cellMap[currentChanging.X, currentChanging.Y].isSource} [Press S To Change]";
        tileEditor.Draw(isSource);

        Text submitText = new Text(mainFont);
        submitText.CharacterSize = 18;
        submitText.FillColor = Color.White;
        submitText.Position = new Vector2f(50, 450);
        submitText.DisplayedString = $"[Enter] to submit changes; [E] to close without saving";
        tileEditor.Draw(submitText);

        Text behaviorText = new Text(mainFont);
        behaviorText.CharacterSize = 18;
        behaviorText.FillColor = Color.White;
        behaviorText.Position = new Vector2f(tileList.Length * 35 + 20, 100);
        behaviorText.DisplayedString = $"[R][G][B][Y][U] To Change Behavior";
        tileEditor.Draw(behaviorText);


        for(int i = 0; i < tileList.Length; i++)
        {
            if(tileList[i].id == "clear")
            {
                continue;
            }
            bool blocked = false, allow = false;
            blocked = currentDisable.Any(e => e == tileList[i].id);
            allow = currentAllow.Any(e => e == tileList[i].id);

            RectangleShape col = new RectangleShape(new Vector2f(30, 30));
            col.FillColor = tileList[i].color;
            col.Position = new Vector2f(45 + ((i - 1) * 35), 100);
            col.OutlineColor = blocked ? Color.Red : allow ? Color.White : Color.Black;
            col.OutlineThickness = 2f;
            tileEditor.Draw(col);
        }
    }

    public static void UpdatePallete()
    {
        palleteCanvas.Clear(Color.Transparent);

        RectangleShape pallete = new RectangleShape(new Vector2f(50, 400));
        pallete.FillColor = new Color(0, 0, 0, 150);
        pallete.Position = new Vector2f(2, 2);
        pallete.OutlineColor = Color.Black;
        pallete.OutlineThickness = 2f;
        palleteCanvas.Draw(pallete);

        List<RectangleShape> tiles = new List<RectangleShape>();
        for(int i = 0; i < tileList.Length; i++)
        {
            var t = tileList[i];
            RectangleShape tl = new RectangleShape(new Vector2f(45, 45));
            tl.FillColor = t.color;
            tl.Position = new Vector2f(4.5f, 2 + (i * 50));
            tl.OutlineColor = Color.Black;
            tl.OutlineThickness = i == selectedTile ? 3f : 1f;
            palleteCanvas.Draw(tl);
        }
    }

    private static Vector2i lastMousePos;
    private static bool isPanning = false;

    public static void InitializeGame()
    {
        mainWindow = new RenderWindow(new VideoMode(new Vector2u(1200, 600)), "Simple Tiles");
        mainWindow.Closed += (sender, e) => mainWindow.Close();

        gameView = new View(new FloatRect(
            new Vector2f(-mainWindow.Size.X / 2, -mainWindow.Size.Y / 2),
            new Vector2f(mainWindow.Size.X, mainWindow.Size.Y)
        ));

        uiView = new View(new FloatRect(
            new Vector2f(0, 0),
            new Vector2f(mainWindow.Size.X, mainWindow.Size.Y)
        ));
        mainWindow.SetView(gameView);

        mainWindow.MouseButtonPressed += (sender, e) =>
        {
            if(e.Button == Mouse.Button.Left)
            {
                ChangeTile();
            }
        };

        mainWindow.MouseButtonPressed += (sender, e) =>
        {
            if(e.Button == Mouse.Button.Right)
            {
                isPanning = true;
                lastMousePos = e.Position;
            }

            if(e.Button == Mouse.Button.Middle)
            {
                ChangeTile(false, true);
            }
        };

        mainWindow.MouseButtonReleased += (sender, e) =>
        {
            if(e.Button == Mouse.Button.Right)
                isPanning = false;
        };

        mainWindow.MouseMoved += (sender, e) =>
        {
            if(isPanning)
            {
                Vector2f delta = new Vector2f(
                    e.Position.X - lastMousePos.X,
                    e.Position.Y - lastMousePos.Y
                );

                gameView.Center -= delta / mainMap.Scale;

                lastMousePos = e.Position;
            }
        };

        mainWindow.Resized += (sender, e) =>
        {
            gameView.Size = new Vector2f(e.Size.X, e.Size.Y);
            gameView.Center = new Vector2f(e.Size.X / 2, e.Size.Y / 2);

            uiView.Size = new Vector2f(e.Size.X, e.Size.Y);
            uiView.Center = new Vector2f(e.Size.X / 2, e.Size.Y / 2);
        };

        mainWindow.MouseWheelScrolled += (sender, e) =>
        {
            if(e.Wheel == Mouse.Wheel.Vertical)
            {
                if(e.Delta > 0)
                {
                    mainMap.Scale += 1f;
                }
                else if(e.Delta < 0 && mainMap.Scale > 2f)
                {
                    mainMap.Scale -= 1f;
                }

                mainMap.position = new SFML.System.Vector2f(-mainMap.Scale * 128 / 2, -mainMap.Scale * 128 / 2);
            }
        };

        mainWindow.KeyPressed += (sender, e) =>
        {
            if(e.Code == Keyboard.Key.Up && selectedTile < tileList.Length - 1)
            {
                selectedTile++;
                UpdatePallete();
            }
            else if(e.Code == Keyboard.Key.Down && selectedTile > 0)
            {
                selectedTile--;
                UpdatePallete();
            }

            if(e.Code == Keyboard.Key.F)
            {
                var cell = GetCurrentTile();
                if(cell != null && cell.tile != null && cell.isSource)
                {
                    cell.isActive = !cell.isActive;
                }
            }

            if(e.Code == Keyboard.Key.E)
            {
                Vector2f worldPos = mainWindow.MapPixelToCoords(Mouse.GetPosition(mainWindow), gameView)
                - mainMap.position;

                int tileX = (int)(worldPos.X / (mainMap.Scale * 10));
                int tileY = (int)(worldPos.Y / (mainMap.Scale * 10));
                if(tileX >= 0 && tileX < mainMap.Tiles.GetLength(0) &&
                    tileY >= 0 && tileY < mainMap.Tiles.GetLength(1))
                {
                    inEditor = !inEditor;
                    currentChanging = new Vector2u((uint)tileX, (uint)tileY);
                    Cell cell = cellMap[tileX, tileY];
                    currentAllow = cell.allow;
                    currentDisable = cell.disabled;
                }

                UpdateEditor();
            }

            if(inEditor)
            {
                if(e.Code == Keyboard.Key.R)
                {
                    ChangeTileBehavior("red");
                }
                if(e.Code == Keyboard.Key.G)
                {
                    ChangeTileBehavior("green");
                }
                if(e.Code == Keyboard.Key.B)
                {
                    ChangeTileBehavior("blue");
                }
                if(e.Code == Keyboard.Key.Y)
                {
                    ChangeTileBehavior("yellow");
                }
                if(e.Code == Keyboard.Key.U)
                {
                    ChangeTileBehavior("universal");
                }
                if(e.Code == Keyboard.Key.S)
                {
                    Cell current = cellMap[currentChanging.X, currentChanging.Y];
                    if(current != null && current.tile != null)
                    {
                        current.isSource = !current.isSource;
                        UpdateEditor();
                    }
                }
                if(e.Code == Keyboard.Key.Enter)
                {
                    Cell cell = cellMap[currentChanging.X, currentChanging.Y];
                    cell.allow = currentAllow;
                    cell.disabled = currentDisable;
                    inEditor = false;
                }
            }
        };

        cellMap = new Cell[128, 128];
        for (int x = 0; x < 128; x++)
        {
            for (int y = 0; y < 128; y++)
            {
                cellMap[x, y] = new Cell
                {
                    tile = null,
                    isActive = false
                };
            }
        }
        mainMap = new TileRenderer(128, 128)
        {
            cellMap = cellMap,
            Scale = 10f
        };
        mainMap.position = new SFML.System.Vector2f(-mainMap.Scale * 128 / 2, -mainMap.Scale * 128 / 2);
        mainMap.UpdateMap();
    }

    public static void ChangeTileBehavior(string key = "red")
    {
        bool blocked = false; bool allow = false;
        blocked = currentDisable.Any(e => e == key);
        allow = currentAllow.Any(e => e == key);

        List<string> a = currentAllow.ToList(), d = currentDisable.ToList();
        if(!blocked && !allow) // Allow
        {
            a.Add(key);
        }
        else if(allow) // Disable
        {
            if(blocked) a.Remove(key);
            d.Add(key);
        }
        else if(blocked) // Ignore
        {
            d.Remove(key);
            if(allow) a.Remove(key);
        }

        currentAllow = a.ToArray();
        currentDisable = d.ToArray();
        UpdateEditor();
    }

    public static void ChangeTile(bool isNull = false, bool isActive = false)
    {
        if(inEditor) return;

        Vector2f worldPos = mainWindow.MapPixelToCoords(Mouse.GetPosition(mainWindow), gameView)
        - mainMap.position;

        int tileX = (int)(worldPos.X / (mainMap.Scale * 10));
        int tileY = (int)(worldPos.Y / (mainMap.Scale * 10));

        if(tileList[selectedTile].id == "clear") isNull = true;

        if(tileX >= 0 && tileX < mainMap.Tiles.GetLength(0) &&
           tileY >= 0 && tileY < mainMap.Tiles.GetLength(1))
        {
            mainMap.SetTile(tileX, tileY, new Cell()
            {
                tile = isNull ? null : tileList[selectedTile],
                isSource = isNull ? false : isActive,
                isUniversal = isNull ? false : tileList[selectedTile].id == "universal",
                allow = isNull ? new string[0] : new string[]
                {
                    tileList[selectedTile].id,
                    "universal"
                }
            });
            mainMap.UpdateMap();
        }
    }

    public static Cell GetCurrentTile()
    {
        Vector2f worldPos = mainWindow.MapPixelToCoords(Mouse.GetPosition(mainWindow), gameView)
        - mainMap.position;

        int tileX = (int)(worldPos.X / (mainMap.Scale * 10));
        int tileY = (int)(worldPos.Y / (mainMap.Scale * 10));

        if(tileX >= 0 && tileX < cellMap.GetLength(0) &&
           tileY >= 0 && tileY < cellMap.GetLength(1))
        {
            return cellMap[tileX, tileY];
        }

        return null;
    }

    public static async Task UpdateCellmap()
    {
        await Task.Run(() =>
        {
            workingOnUpdate = true;

            Cell[,] copy = (Cell[,])cellMap.Clone();
            int w = copy.GetLength(0);
            int h = copy.GetLength(1);

            bool[,] visited = new bool[w, h];
            Queue<(int x, int y)> queue = new();

            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                if (copy[x, y].isSource && copy[x, y].tile != null && copy[x, y].isActive)
                {
                    queue.Enqueue((x, y));
                    copy[x, y].isConnectedToSource = true;
                    copy[x, y].isActive = true;
                }
                else
                {
                    copy[x, y].isConnectedToSource = false;
                    copy[x, y].isActive = false;
                }
            }

            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();
                if (visited[cx, cy]) continue;
                visited[cx, cy] = true;

                var current = copy[cx, cy];

                (int dx, int dy)[] dirs =
                {
                    (-1, 0), (1, 0), (0, -1), (0, 1)
                };

                current.isBlocked = false;
                foreach (var (dx, dy) in dirs)
                {
                    int nx = cx + dx;
                    int ny = cy + dy;

                    if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;

                    var neighbor = copy[nx, ny];
                    if (neighbor.tile == null) continue;

                    if (current.disabled.Any(e => e == neighbor.tile.id) && neighbor.isActive)
                    {
                        current.isBlocked = true;
                        current.isActive = false;
                        current.isConnectedToSource = false;
                    }
                }

                if (current.isBlocked) continue;

                foreach (var (dx, dy) in dirs)
                {
                    int nx = cx + dx;
                    int ny = cy + dy;

                    if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;

                    var neighbor = copy[nx, ny];
                    if (neighbor.tile == null) continue;

                    if (neighbor.isSource) continue;

                    if (!neighbor.isConnectedToSource && CheckCell(current, neighbor) && current.isActive)
                    {
                        neighbor.isConnectedToSource = true;
                        neighbor.isActive = true;
                        queue.Enqueue((nx, ny));
                    }
                }
            }

            cellMap = copy;
            mainMap.cellMap = cellMap;

            updateMap = true;
            workingOnUpdate = false;
        });
    }

    public static bool CheckCell(Cell cell, Cell neighboor)
    {
        if(neighboor == null || neighboor.tile == null) return false;

        bool canConnect = false;

        if (cell.allow.Length > 0)
        {
            canConnect = cell.allow.Any(e => e == neighboor.tile.id);
        }
        else
        {
            canConnect = cell.tile.id == neighboor.tile.id;
        }

        if (canConnect && cell.disabled.Length > 0)
        {
            canConnect = !cell.disabled.Any(e => e == neighboor.tile.id);
        }

        return canConnect;
    }
}