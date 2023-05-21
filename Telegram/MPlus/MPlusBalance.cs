namespace Telegram.MPlus;

internal record MPlusBalance(int Balance, int RealBalance, int EarnBalance)
{
    internal int BonusBalance => Balance - RealBalance;
}
