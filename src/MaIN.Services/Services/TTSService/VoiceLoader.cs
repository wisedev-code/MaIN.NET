using TorchSharp;

namespace MaIN.Services.Services.TTSService;

public static class VoiceLoader
{
    public static float[,,] TensorToArray3D(string path, (int width, int height, int depth) dimensions)
    {
        byte[] bytes = File.ReadAllBytes(path);
        float[,,] result = new float[dimensions.width, dimensions.height, dimensions.depth];
        
        int index = 0;
        for (int x = 0; x < dimensions.width; x++)
        {
            for (int y = 0; y < dimensions.height; y++)
            {
                for (int z = 0; z < dimensions.depth; z++)
                {
                    result[x, y, z] = BitConverter.ToSingle(bytes, index);
                    index += 4;
                }
            }
        }
        
        return result;
    }
}