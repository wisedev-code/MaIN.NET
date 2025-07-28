// using System.Text.Json;
// using MaIN.Services.Services.Abstract;
// using MaIN.Services.Services.TTSService.Models;
// using Microsoft.ML.OnnxRuntime;
// using Microsoft.ML.OnnxRuntime.Tensors;
// using NAudio.Wave;
// using NumSharp;
//
// namespace MaIN.Services.Services.TTSService;
//
// public class TTSEnginge : ITTSEnginge
// {
//     private InferenceSession session;
//     private readonly Dictionary<string, VoiceConfig> voices;
//     private readonly TokenizerConfig tokenizerConfig;
//     private bool disposed = false;
//
//     public TTSEnginge(string modelPath, string voicesPath = null, string tokenizerConfigPath = null)
//     {
//         voices = new Dictionary<string, VoiceConfig>();
//         tokenizerConfig = LoadTokenizerConfig(tokenizerConfigPath);
//         
//         InitializeSession(modelPath);
//         LoadVoices(voicesPath ?? Path.Combine(Path.GetDirectoryName(modelPath), "voices"));
//     }
//
//     public void InitializeSession(string modelPath)
//     {
//         var sessionOptions = new SessionOptions();
//         
//         // Optimize for performance
//         sessionOptions.InterOpNumThreads = Environment.ProcessorCount;
//         sessionOptions.IntraOpNumThreads = Environment.ProcessorCount;
//         sessionOptions.ExecutionMode = ExecutionMode.ORT_PARALLEL;
//         sessionOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
//         
//         // Enable GPU if available (uncomment for CUDA)
//         // sessionOptions.AppendExecutionProvider_CUDA(0);
//         
//         session = new InferenceSession(modelPath, sessionOptions);
//         
//         Console.WriteLine("Model loaded successfully!");
//         PrintModelInfo();
//     }
//     
//     private void PrintModelInfo()
//     {
//         Console.WriteLine("Input metadata:");
//         foreach (var input in session.InputMetadata)
//         {
//             Console.WriteLine($"  {input.Key}: {string.Join(", ", input.Value.Dimensions)} ({input.Value.ElementType})");
//         }
//         
//         Console.WriteLine("Output metadata:");
//         foreach (var output in session.OutputMetadata)
//         {
//             Console.WriteLine($"  {output.Key}: {string.Join(", ", output.Value.Dimensions)} ({output.Value.ElementType})");
//         }
//     }
//     
//     private TokenizerConfig LoadTokenizerConfig(string configPath)
//     {
//         if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
//         {
//             return CreateDefaultTokenizerConfig();
//         }
//         
//         var json = File.ReadAllText(configPath);
//         return JsonSerializer.Deserialize<TokenizerConfig>(json);
//     }
//     
//     private TokenizerConfig CreateDefaultTokenizerConfig()
//     {
//         return new TokenizerConfig
//         {
//             VocabSize = 256,
//             PadToken = 0,
//             BosToken = 1,
//             EosToken = 2,
//             UnkToken = 3
//         };
//     }
//     
//     private void LoadVoices(string voicesPath)
//     {
//         if (!Directory.Exists(voicesPath))
//         {
//             Console.WriteLine($"Voices directory not found: {voicesPath}");
//             return;
//         }
//
//         foreach (var voiceFile in Directory.GetFiles(voicesPath, "*.npy"))
//         {
//             try
//             {
//                 var voiceName = Path.GetFileNameWithoutExtension(voiceFile);
//                 var voiceFeatures = np.Load<float[,,]>(voiceFile);
//                 
//                 voices[voiceName] = voiceFeatures;
//                 Console.WriteLine($"Loaded voice: {voiceName} (embedding size: {voiceFeatures.Length})");
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine($"Failed to load voice {voiceFile}: {ex.Message}");
//             }
//         }
//     }
//     
//     public async Task<float[]> GenerateAudioAsync(string text, string voiceName = "af_heart", 
//         float speed = 1.0f, float pitch = 1.0f)
//     {
//         if (!voices.ContainsKey(voiceName))
//         {
//             throw new ArgumentException($"Voice '{voiceName}' not found. Available voices: {string.Join(", ", voices.Keys)}");
//         }
//
//         var voice = voices[voiceName];
//         
//         // Tokenize the input text
//         var tokens = TokenizeText(text);
//         
//         // Prepare input tensors
//         var inputs = PrepareInputTensors(tokens, voice, speed, pitch);
//         
//         // Run inference
//         using var results = await Task.Run(() => session.Run(inputs));
//         
//         // Extract audio from results
//         var audioTensor = results.First().AsTensor<float>();
//         return audioTensor.ToArray();
//     }
//     
//     private int[] TokenizeText(string text)
//     {
//         // Basic tokenization - you might want to implement proper phonemization here
//         var tokens = new List<int> { tokenizerConfig.BosToken };
//         
//         foreach (char c in text.ToLower())
//         {
//             if (char.IsLetter(c))
//             {
//                 tokens.Add(c - 'a' + 4); // Simple character mapping
//             }
//             else if (char.IsWhiteSpace(c))
//             {
//                 tokens.Add(tokenizerConfig.PadToken);
//             }
//             else
//             {
//                 tokens.Add(tokenizerConfig.UnkToken);
//             }
//         }
//         
//         tokens.Add(tokenizerConfig.EosToken);
//         return tokens.ToArray();
//     }
//     
//     private List<NamedOnnxValue> PrepareInputTensors(int[] tokens, VoiceConfig voice, 
//         float speed, float pitch)
//     {
//         var inputs = new List<NamedOnnxValue>();
//
//         // Input IDs tensor
//         var inputIds = new DenseTensor<long>(tokens.Select(t => (long)t).ToArray(), new[] { 1, tokens.Length });
//         inputs.Add(NamedOnnxValue.CreateFromTensor("input_ids", inputIds));
//
//         // Voice embedding tensor
//         var voiceEmbedding = new DenseTensor<float>(voice.Features, new[] { 1, voice.Features.Length });
//         inputs.Add(NamedOnnxValue.CreateFromTensor("voice", voiceEmbedding));
//
//         // Speed control
//         var speedTensor = new DenseTensor<float>(new[] { speed }, new[] { 1 });
//         inputs.Add(NamedOnnxValue.CreateFromTensor("speed", speedTensor));
//
//         // Pitch control (if supported by the model)
//         if (session.InputMetadata.ContainsKey("pitch"))
//         {
//             var pitchTensor = new DenseTensor<float>(new[] { pitch }, new[] { 1 });
//             inputs.Add(NamedOnnxValue.CreateFromTensor("pitch", pitchTensor));
//         }
//
//         return inputs;
//     }
//     
//     public void SaveAudioToWav(float[] audioData, string outputPath, int sampleRate = 24000)
//     {
//         using var writer = new WaveFileWriter(outputPath, new WaveFormat(sampleRate, 16, 1));
//         
//         // Convert float samples to 16-bit PCM
//         foreach (var sample in audioData)
//         {
//             var intSample = (short)(sample * short.MaxValue);
//             writer.WriteSample(intSample);
//         }
//     }
//
//     public void PlayAudio(float[] audioData, int sampleRate = 24000)
//     {
//         using var waveOut = new WaveOutEvent();
//         using var provider = new RawSourceWaveStream(
//             new MemoryStream(ConvertToBytes(audioData)), 
//             new WaveFormat(sampleRate, 16, 1));
//         
//         waveOut.Init(provider);
//         waveOut.Play();
//         
//         while (waveOut.PlaybackState == PlaybackState.Playing)
//         {
//             System.Threading.Thread.Sleep(100);
//         }
//     }
//
//     private byte[] ConvertToBytes(float[] audioData)
//     {
//         var bytes = new byte[audioData.Length * 2];
//         for (int i = 0; i < audioData.Length; i++)
//         {
//             var sample = (short)(audioData[i] * short.MaxValue);
//             bytes[i * 2] = (byte)(sample & 0xFF);
//             bytes[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
//         }
//         return bytes;
//     }
//
//     public IEnumerable<string> GetAvailableVoices()
//     {
//         return voices.Keys;
//     }
//
//     public void Dispose()
//     {
//         if (!disposed)
//         {
//             session?.Dispose();
//             disposed = true;
//         }
//     }
// }