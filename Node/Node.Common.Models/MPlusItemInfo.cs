namespace Node.Common.Models;

/*
{
   id: t.string,
   iid: t.string,
   state: cContentItemState,
   directory: t.string,
   metadata: cAugmentedItemMetadata,
   media: {
       jpeg?: cJPEGMediaInfo,
       eps?: cEPSMetadata,
       mov?: cMOVParameters
   },
   previewurl: t.string,
   thumbnailurl: t.string,

   type?: cContentType,
   videopreview?: cContentVideoPreview,
   qspreview?: cQwertyStockPreview,
   nowmpreviewurl?: t.string
}
*/
public record MPlusItemInfo(string Iid, string PreviewUrl) { }
