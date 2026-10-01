using System.Text;

namespace TagWriter.Services;

public static class TypewriterSoundFactory
{
    const int SampleRate = 44100;

    public static string EnsureSoundSet()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TagWriter", "Sounds", "typewriter-v1");
        Directory.CreateDirectory(dir);

        var keyParams = new (int seed, double f1, double f2)[]
        {
            (101,1320,2130),(102,1450,2260),(103,1580,2410),(104,1710,2550),
            (105,1860,2690),(106,2010,2830),(107,2180,2990),(108,2350,3180)
        };
        for (var i = 0; i < keyParams.Length; i++)
            Ensure(Path.Combine(dir, $"key{i + 1}.wav"), GenerateKey(keyParams[i].seed, keyParams[i].f1, keyParams[i].f2));

        Ensure(Path.Combine(dir, "space.wav"), GenerateSpace());
        Ensure(Path.Combine(dir, "backspace.wav"), GenerateBackspace());
        Ensure(Path.Combine(dir, "enter.wav"), GenerateEnter());
        return dir;
    }

    static void Ensure(string path, float[] samples)
    {
        if (File.Exists(path) && new FileInfo(path).Length > 1000) return;
        WriteWav(path, samples);
    }

    static float[] GenerateKey(int seed, double f1, double f2)
    {
        const double duration = .072;
        var random = new Random(seed);
        var data = new float[(int)(SampleRate * duration)];
        for (var i = 0; i < data.Length; i++)
        {
            var t = i / (double)SampleRate;
            var env = Math.Exp(-t * 55);
            var noise = (random.NextDouble() * 2 - 1) * env;
            var metallic = (Math.Sin(2 * Math.PI * f1 * t) + .52 * Math.Sin(2 * Math.PI * f2 * t)) * Math.Exp(-t * 38);
            var click2 = 0.0;
            if (t >= .011)
            {
                var t2 = t - .011;
                click2 = (random.NextDouble() * 2 - 1) * Math.Exp(-t2 * 90);
            }
            data[i] = (float)(.86 * (.45 * noise + .42 * metallic + .13 * click2));
        }
        return data;
    }

    static float[] GenerateSpace()
    {
        var random = new Random(300);
        var data = new float[(int)(SampleRate * .085)];
        for (var i = 0; i < data.Length; i++)
        {
            var t = i / (double)SampleRate;
            var env = Math.Exp(-t * 42);
            var noise = (random.NextDouble() * 2 - 1) * env;
            var tone = Math.Sin(2 * Math.PI * 240 * t) * Math.Exp(-t * 30);
            data[i] = (float)(.60 * (.68 * noise + .32 * tone));
        }
        return data;
    }

    static float[] GenerateBackspace()
    {
        var random = new Random(400);
        var data = new float[(int)(SampleRate * .12)];
        for (var i = 0; i < data.Length; i++)
        {
            var t = i / (double)SampleRate;
            var value = 0.0;
            foreach (var hit in new[] { (start: 0.0, freq: 1150.0), (start: .034, freq: 1500.0) })
            {
                if (t < hit.start) continue;
                var tt = t - hit.start;
                var env = Math.Exp(-tt * 70);
                value += .38 * (random.NextDouble() * 2 - 1) * env + .28 * Math.Sin(2 * Math.PI * hit.freq * tt) * Math.Exp(-tt * 45);
            }
            data[i] = (float)value;
        }
        return data;
    }

    static float[] GenerateEnter()
    {
        var random = new Random(500);
        var data = new float[(int)(SampleRate * .34)];
        var starts = new[] { .035, .065, .095, .125, .155 };
        for (var i = 0; i < data.Length; i++)
        {
            var t = i / (double)SampleRate;
            var value = .45 * (random.NextDouble() * 2 - 1) * Math.Exp(-t * 65);
            for (var k = 0; k < starts.Length; k++)
            {
                if (t < starts[k]) continue;
                var tt = t - starts[k];
                value += .12 * (random.NextDouble() * 2 - 1) * Math.Exp(-tt * 100);
                value += .08 * Math.Sin(2 * Math.PI * (800 + 80 * k) * tt) * Math.Exp(-tt * 70);
            }
            if (t >= .12)
            {
                var tt = t - .12;
                value += .34 * Math.Sin(2 * Math.PI * 2100 * tt) * Math.Exp(-tt * 9);
                value += .15 * Math.Sin(2 * Math.PI * 3150 * tt) * Math.Exp(-tt * 12);
            }
            data[i] = (float)value;
        }
        return data;
    }

    static void WriteWav(string path, float[] samples)
    {
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        var dataLength = samples.Length * 2;

        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataLength);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(SampleRate);
        writer.Write(SampleRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataLength);

        foreach (var sample in samples)
        {
            var value = (short)(Math.Clamp(sample, -1f, 1f) * short.MaxValue);
            writer.Write(value);
        }
    }
}
