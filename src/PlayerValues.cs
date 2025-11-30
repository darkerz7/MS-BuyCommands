namespace MS_BuyCommands
{
    public class PlayerValues
    {
        public Dictionary<string, int> NumBuyCT = [];
        public Dictionary<string, int> NumBuyT = [];
        Guid? Timer = null;

        public bool IsCanBuy()
        {
            if (Timer == null) return true;
            return false;
        }

        public void SetCooldown (float Delay)
        {
            KillTimer();
            Timer = BuyCommands._modSharp!.PushTimer(() => KillTimer(), Delay);
        }

        void KillTimer()
        {
            if (Timer != null)
            {
                BuyCommands._modSharp!.StopTimer((Guid)Timer);
                Timer = null;
            }
        }
    }
}
