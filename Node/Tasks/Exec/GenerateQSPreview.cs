namespace Node.Tasks.Exec;

public class QSPreviewInfo { }

public static class GenerateQSPreviewTasks
{
    public static IEnumerable<IPluginAction> CreateTasks() => new IPluginAction[] { new GenerateQSPreview() };


    class GenerateQSPreview : FFMpegTasks.FFMpegAction<QSPreviewInfo>
    {
        public override TaskAction Name => TaskAction.GenerateQSPreview;
        public override TaskFileFormatRequirements InputRequirements { get; } = new TaskFileFormatRequirements(FileFormat.Jpeg).MaybeOne(FileFormat.Mov);
        public override TaskFileFormatRequirements OutputRequirements { get; } = new TaskFileFormatRequirements(FileFormat.Jpeg).MaybeOne(FileFormat.Mov);

        protected override void ConstructFFMpegArguments(ReceivedTask task, QSPreviewInfo data, in FFMpegArgsHolder args)
        {
            var watermarkFile = cacheRepeateWatermark().GetAwaiter().GetResult();
            args.Args.Add("-i", watermarkFile);

            var graph = "";

            // scale video to 640px by width
            graph += "scale= w=ceil(in_w/in_h*640/2)*2:h=640 [v];";

            // add watermark onto the base video/image
            graph += "[v][1] overlay= (main_w-overlay_w)/2:(main_h-overlay_h)/2:format=auto,";

            // set the color format
            graph += "format= yuv420p";

            args.Filtergraph.AddLast(graph);
            args.Args.Add("-an");


            async Task<string> cacheRepeateWatermark()
            {
                var watermarkFile = "assets/qs_watermark.png";
                var repeatedWatermarkFile = Path.Combine(Init.RuntimeCacheDirectory("ffmpeg"), Path.GetFileName(watermarkFile));
                if (File.Exists(repeatedWatermarkFile)) return repeatedWatermarkFile;


                var graph = "";

                // repeat watermark several times vertically and horizontally
                graph += "[0][0] hstack, split, vstack," + string.Join(string.Empty, Enumerable.Repeat("split, hstack, split, vstack,", 2));

                // rotate watermark -20 deg
                graph += "rotate= -20*PI/180:fillcolor=none:ow=rotw(iw):oh=roth(ih), format= rgba";

                var argholder = new FFMpegArgsHolder(FileFormat.Png, null);
                argholder.Filtergraph.Add(graph);

                var ffargs = FFMpegExec.GetFFMpegArgs(watermarkFile, repeatedWatermarkFile, task, false, argholder);
                await ExecuteProcess(task.GetPlugin().GetInstance().Path, ffargs, delegate { }, task, stderr: LogLevel.Trace);

                return repeatedWatermarkFile;
            }
        }
    }
}