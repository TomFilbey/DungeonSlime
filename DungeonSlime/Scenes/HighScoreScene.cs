using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Scenes;
using MonoGameLibrary.Graphics;
using MonoGameLibrary;
using DungeonSlime.Services;
using DungeonSlime.UI;
using System.Collections.Generic;
using MonoGameGum;
using MonoGameGum.Forms.Controls;

namespace DungeonSlime.Scenes
{
    public class HighScoreScene : Scene
    {
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;
        private DifficultyMode _currentDifficulty;
        private List<HighScoreEntry> _currentScores;
        private Texture2D _backgroundPattern;
        
        // Background scrolling variables
        private Rectangle _backgroundDestination;
        private Vector2 _backgroundOffset;
        private float _scrollSpeed = 50.0f;

        // UI Elements
        private Panel _mainPanel;
        private AnimatedButton _easyButton;
        private AnimatedButton _mediumButton;
        private AnimatedButton _hardButton;
        private AnimatedButton _backButton;
        private TextureAtlas _atlas;

        public HighScoreScene()
        {
            _currentDifficulty = DifficultyMode.Easy;
            LoadScores();
        }

        public override void Initialize()
        {
            base.Initialize();
            
            // Set the background pattern destination rectangle to fill the entire screen
            _backgroundDestination = Core.GraphicsDevice.PresentationParameters.Bounds;
            
            // Initialize the offset of the background pattern at zero
            _backgroundOffset = Vector2.Zero;
            
            InitializeUI();
        }

        public override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(Core.Graphics.GraphicsDevice);
            _font = Core.Content.Load<SpriteFont>("fonts/04B_30");
            _backgroundPattern = Core.Content.Load<Texture2D>("images/background-pattern");
            _atlas = TextureAtlas.FromFile(Core.Content, "images/atlas-definition.xml");
        }

        private void InitializeUI()
        {
            // Clear out any previous UI
            GumService.Default.Root.Children.Clear();

            // Create main panel
            _mainPanel = new Panel();
            _mainPanel.Dock(Gum.Wireframe.Dock.Fill);
            _mainPanel.AddToRoot();

            // Easy button
            _easyButton = new AnimatedButton(_atlas);
            _easyButton.Text = "EASY";
            _easyButton.Anchor(Gum.Wireframe.Anchor.Left);
            _easyButton.Visual.X = 50;
            _easyButton.Visual.Y = 60;
            _easyButton.Click += HandleEasyClicked;
            _mainPanel.AddChild(_easyButton);

            // Medium button
            _mediumButton = new AnimatedButton(_atlas);
            _mediumButton.Text = "MEDIUM";
            _mediumButton.Anchor(Gum.Wireframe.Anchor.Center);
            _mediumButton.Visual.X = 0;
            _mediumButton.Visual.Y = 60;
            _mediumButton.Click += HandleMediumClicked;
            _mainPanel.AddChild(_mediumButton);

            // Hard button
            _hardButton = new AnimatedButton(_atlas);
            _hardButton.Text = "HARD";
            _hardButton.Anchor(Gum.Wireframe.Anchor.Right);
            _hardButton.Visual.X = -50;
            _hardButton.Visual.Y = 60;
            _hardButton.Click += HandleHardClicked;
            _mainPanel.AddChild(_hardButton);

            // Back button
            _backButton = new AnimatedButton(_atlas);
            _backButton.Text = "BACK";
            _backButton.Anchor(Gum.Wireframe.Anchor.BottomRight);
            _backButton.Visual.X = -20f;
            _backButton.Visual.Y = -5;
            _backButton.Click += HandleBackClicked;
            _mainPanel.AddChild(_backButton);

            // Set initial focus and update button states
            UpdateButtonFocus();
        }

        public override void Update(GameTime gameTime)
        {
            // Update the background scrolling animation
            float offset = _scrollSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
            _backgroundOffset.X -= offset;
            _backgroundOffset.Y -= offset;

            // Ensure that the offsets do not go beyond the texture bounds for seamless wrap
            _backgroundOffset.X %= _backgroundPattern.Width;
            _backgroundOffset.Y %= _backgroundPattern.Height;

            // Update Gum UI
            GumService.Default.Update(gameTime);
        }

        private void HandleEasyClicked(object sender, System.EventArgs e)
        {
            _currentDifficulty = DifficultyMode.Easy;
            LoadScores();
            UpdateButtonFocus();
        }

        private void HandleMediumClicked(object sender, System.EventArgs e)
        {
            _currentDifficulty = DifficultyMode.Medium;
            LoadScores();
            UpdateButtonFocus();
        }

        private void HandleHardClicked(object sender, System.EventArgs e)
        {
            _currentDifficulty = DifficultyMode.Hard;
            LoadScores();
            UpdateButtonFocus();
        }

        private void HandleBackClicked(object sender, System.EventArgs e)
        {
            Core.ChangeScene(new TitleScene());
        }

        private void UpdateButtonFocus()
        {
            // Clear all focus
            _easyButton.IsFocused = false;
            _mediumButton.IsFocused = false;
            _hardButton.IsFocused = false;

            // Set focus based on current difficulty
            switch (_currentDifficulty)
            {
                case DifficultyMode.Easy:
                    _easyButton.IsFocused = true;
                    break;
                case DifficultyMode.Medium:
                    _mediumButton.IsFocused = true;
                    break;
                case DifficultyMode.Hard:
                    _hardButton.IsFocused = true;
                    break;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            Core.Graphics.GraphicsDevice.Clear(new Color(32, 40, 78, 255));

            // Draw the scrolling background pattern using PointWrap sampler state
            _spriteBatch.Begin(samplerState: SamplerState.PointWrap);
            _spriteBatch.Draw(_backgroundPattern, _backgroundDestination, 
                new Rectangle(_backgroundOffset.ToPoint(), _backgroundDestination.Size), 
                Color.White * 0.5f);
            _spriteBatch.End();

            // Draw the UI text elements
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            // Draw title
            string title = "HIGH SCORES";
            Vector2 titleSize = _font.MeasureString(title);
            Vector2 titlePos = new Vector2(Core.Graphics.GraphicsDevice.Viewport.Width / 2 - titleSize.X / 2, 10);
            
            // Draw title with drop shadow
            Color dropShadowColor = Color.Black * 0.5f;
            _spriteBatch.DrawString(_font, title, titlePos + new Vector2(5, 5), dropShadowColor);
            _spriteBatch.DrawString(_font, title, titlePos, Color.Yellow);

            // Draw difficulty title
            string difficultyTitle = $"{_currentDifficulty.ToString().ToUpper()} MODE";
            Vector2 difficultySize = _font.MeasureString(difficultyTitle);
            Vector2 difficultyPos = new Vector2(Core.Graphics.GraphicsDevice.Viewport.Width / 2 - difficultySize.X / 2, 100);
            
            Color difficultyColor = _currentDifficulty switch
            {
                DifficultyMode.Easy => Color.Green,
                DifficultyMode.Medium => Color.Orange,
                DifficultyMode.Hard => Color.Red,
                _ => Color.White
            };
            
            _spriteBatch.DrawString(_font, difficultyTitle, difficultyPos, difficultyColor);

            // Draw scores
            DrawScores();

            _spriteBatch.End();

            // Draw the Gum UI (buttons)
            GumService.Default.Draw();
        }

        private void DrawScores()
        {
            if (_currentScores == null || _currentScores.Count == 0)
            {
                string noScores = "No scores yet! Play some games!";
                Vector2 noScoresSize = _font.MeasureString(noScores);
                Vector2 noScoresPos = new Vector2(Core.Graphics.GraphicsDevice.Viewport.Width / 2 - noScoresSize.X / 2, 250);
                _spriteBatch.DrawString(_font, noScores, noScoresPos, Color.Gray);
                return;
            }

            float startY = 170;
            float lineHeight = 35;

            // Draw header
            string header = "RANK  SCORE     PLAYER     DATE";
            Vector2 headerPos = new Vector2(100, startY);
            _spriteBatch.DrawString(_font, header, headerPos, Color.Yellow);

            // Draw scores
            for (int i = 0; i < _currentScores.Count; i++)
            {
                var entry = _currentScores[i];
                float y = startY + lineHeight + (i * lineHeight);

                // Rank
                string rank = $"{i + 1:D2}.";
                _spriteBatch.DrawString(_font, rank, new Vector2(100, y), Color.White);

                // Score
                string score = entry.Score.ToString();
                _spriteBatch.DrawString(_font, score, new Vector2(170, y), Color.Cyan);

                // Player name (truncated if too long)
                string playerName = entry.PlayerName;
                if (playerName.Length > 10)
                    playerName = playerName.Substring(0, 10) + "...";
                _spriteBatch.DrawString(_font, playerName, new Vector2(300, y), Color.LightGreen);

                // Date
                string date = entry.Date.ToString("MM/dd/yy");
                _spriteBatch.DrawString(_font, date, new Vector2(500, y), Color.LightGray);
            }
        }

        private void LoadScores()
        {
            _currentScores = HighScoreService.GetHighScores(_currentDifficulty);
        }
    }
}
