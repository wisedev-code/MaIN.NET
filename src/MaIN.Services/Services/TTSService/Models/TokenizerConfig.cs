namespace MaIN.Services.Services.TTSService.Models;

public class TokenizerConfig
{
    public int VocabSize { get; set; }
    public int PadToken { get; set; }
    public int BosToken { get; set; }
    public int EosToken { get; set; }
    public int UnkToken { get; set; }
}