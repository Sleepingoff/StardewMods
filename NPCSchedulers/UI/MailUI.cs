using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NPCSchedulers.Store;
using StardewValley;
using StardewValley.Menus;

namespace NPCSchedulers.UI
{

    public class MailUI : UIBase
    {
        private UIStateManager uiStateManager;
        private Vector2 position;
        private ClickableTextureComponent checkBox;
        public Rectangle Bounds => checkBox.bounds;
        private string mailKey;
        private string mailValue;
        private bool mailCondition;
        public MailUI(Vector2 position, string mailKey, string mailValue, bool mailCondition, UIStateManager uiStateManager)
        {
            this.uiStateManager = uiStateManager;
            this.position = position;
            this.mailKey = mailKey;
            this.mailValue = mailValue;
            this.mailCondition = mailCondition;
            int checkValue = mailCondition ? 226 + 9 : 226;
            checkBox = new ClickableTextureComponent(
                new Rectangle((int)position.X + 100, (int)position.Y, 300, 30),
                Game1.mouseCursors,
                new Rectangle(checkValue, 424, 9, 9),
                4f
            );
        }
        public override bool Draw(SpriteBatch b)
        {
            checkBox.draw(b);
            checkBox.bounds.X = (int)position.X + 100;
            checkBox.bounds.Y = (int)position.Y;
            b.DrawString(Game1.smallFont, mailKey, new Vector2((int)position.X + 150, checkBox.bounds.Center.Y - 10), Color.Gray);
            return false;
        }

        public override void LeftClick(int x, int y)
        {
            if (checkBox.containsPoint(x, y))
            {
                //해당 mailKey를 추가하고, 해당 mailKey의 상태를 변경한다.

                uiStateManager.SetMailCondition(mailKey, !mailCondition);

                Game1.playSound("coin");

            }
        }

        public void DrawTooltip(SpriteBatch b)
        {
            Rectangle Bounds = new Rectangle((int)position.X + 150, checkBox.bounds.Center.Y - 10, 300, 50);
            SchedulePage.DrawTooltip(b, mailValue, Bounds);
        }
    }
    public class MailTargetUI : UIBase
    {
        private string scheduleKey;
        public Vector2 position;
        private UIStateManager uiStateManager;
        public int Height = 50;
        public Rectangle Bounds;
        public bool IsClicked = true;
        public bool receivedMail = false;
        public MailTargetUI(Vector2 position, string scheduleKey, UIStateManager uiStateManager)
        {
            this.scheduleKey = scheduleKey;
            this.uiStateManager = uiStateManager;
            this.position = new Vector2(position.X, position.Y - Height);

        }
        public override bool Draw(SpriteBatch b)
        {

            if (!IsVisible) return true;
            var conditions = uiStateManager.GetMailCondition();
            var filteredCondition = MailUIStateHandler.FilterData(conditions);
            int yOffset = 0;

            foreach (var condition in filteredCondition)
            {
                ClickableTextureComponent mailButton = new ClickableTextureComponent(
                    new Rectangle((int)position.X, (int)position.Y + yOffset, 32, 32),
                    Game1.mouseCursors, new Rectangle(188, 422, 16, 16), 2f);
                mailButton.draw(b);

                receivedMail = uiStateManager.GetReceivedMail(condition.Key);
                Color stringColor = receivedMail ? Color.Green : Color.Red;
                b.DrawString(Game1.smallFont, $"{condition.Key}", new Vector2(position.X + 50, (int)position.Y + yOffset), stringColor);
                yOffset += 30;
            }
            Height = yOffset + 50;
            Bounds = new Rectangle((int)position.X, (int)position.Y, 400, Height);
            return false;
        }

        public override void LeftClick(int x, int y)
        {
            if (Bounds.Contains(x, y))
            {
                IsClicked = !IsClicked;
                Game1.playSound("coin");
            }
        }
    }
    public class MailListUI : ListUI
    {
        UIStateManager uiStateManager;
        List<MailUI> mailUIs = new();
        Dictionary<string, string> mailList = new();
        public MailListUI(Vector2 position, UIStateManager uiStateManager) : base(position, 400, 400)
        {
            this.uiStateManager = uiStateManager;
            this.position = position;
            this.mailList = DataLoader.Mail(Game1.content);
            UpdateMailUI();
        }
        public void UpdateMailUI()
        {
            mailUIs.Clear();
            var mailCondition = uiStateManager.GetMailCondition();
            this.mailList = uiStateManager.GetMailList();
            int yOffset = 20;
            foreach (var mail in mailList)
            {
                bool isChecked = mailCondition.ContainsKey(mail.Key) ? mailCondition[mail.Key] : false;
                string result = mail.Value.Length > 10 ? mail.Value.Substring(0, 10) + "..." : mail.Value;
                //issue yOffset이 모든 mailUI에 동일하게 적용됨. 즉, 가장 마지막의 detailDisplayPosition에 모든 ui가 위치함.
                //-> static 을 지움

                var detailDisplayPosition = new Vector2(position.X, position.Y + yOffset - scrollPosition);

                mailUIs.Add(new MailUI(detailDisplayPosition, mail.Key, result, isChecked, uiStateManager));

                yOffset += 50;
            }
            SetMaxScrollPosition(yOffset, viewport.Height);
        }
        public override bool Draw(SpriteBatch b)
        {
            b.DrawString(Game1.dialogueFont, "Mail List", new Vector2(position.X + 150, position.Y - 50), Color.Black);
            base.Draw(b);
            UpdateMailUI();
            foreach (var ui in mailUIs)
            {
                ui.Draw(b);
            }
            base.DrawEnd(b);
            foreach (var ui in mailUIs)
            {
                ui.DrawTooltip(b);
            }

            return false;
        }
        public override void Scroll(int direction)
        {
            base.Scroll(direction);
        }
        public override void LeftClick(int x, int y)
        {
            if (upArrow.containsPoint(x, y))
            {
                Scroll(-1); UpdateMailUI();
            }
            else if (downArrow.containsPoint(x, y))
            {
                Scroll(1); UpdateMailUI();
            }
            else
            {

                foreach (var mailUI in mailUIs)
                {
                    if (mailUI.Bounds.Contains(x, y))
                        mailUI.LeftClick(x, y);
                }
            }
        }
    }
}