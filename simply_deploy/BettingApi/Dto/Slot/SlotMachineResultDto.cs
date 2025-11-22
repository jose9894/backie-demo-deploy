namespace BettingApi.Dto
{
    public class SlotMachineResultDto
    {
        public string[][] FinalGrid {get; set;} = [];
        public int Payout { get; set; }        
    }
}