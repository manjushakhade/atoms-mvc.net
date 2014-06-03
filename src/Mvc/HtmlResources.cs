﻿using NeuroSpeech.Atoms.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;

namespace System.Web.Mvc {
    public static class HtmlResourcesHelper {

        /// <summary>
        /// Register resource to be rendered on this page
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="resource"></param>
        public static void Register(this HtmlHelper helper, HtmlResource resource)
        {
            Register(helper.ViewContext.HttpContext, resource);
        }

        /// <summary>
        /// Register resource to be rendered on this page
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="resource"></param>
        public static void Register(this Controller controller, HtmlResource resource)
        {
            Register(controller.HttpContext, resource);
        }

        private static void Register(this HttpContextBase context, HtmlResource resource) {
            var rs = context.Items["HtmlResources"] as List<HtmlResource>;
            if (rs == null)
            {
                rs = new List<HtmlResource>();
                context.Items["HtmlResources"] = rs;
            }

            if (rs.Contains(resource))
                return;

            rs.Add(resource);
        }


        /// <summary>
        /// Render all registered resources, this must be used only inside a layout page or on the page without layout
        /// </summary>
        /// <param name="helper"></param>
        public static HelperResult RenderResources<T>(this HtmlHelper<T> helper) {
            return RenderResources((HtmlHelper)helper);
        }

        /// <summary>
        /// Render all registered resources, this must be used only inside a layout page or on the page without layout
        /// </summary>
        /// <param name="helper"></param>
        public static HelperResult RenderResources(this HtmlHelper helper)
        {

            return new HelperResult(sw =>
            {

                var rs = helper.ViewContext.HttpContext.Items["HtmlResources"] as List<HtmlResource>;
                if (rs == null)
                {
                    return;
                }

                List<HtmlResource> result = new List<HtmlResource>();
                Build(rs, result);

                StringBuilder sb = new StringBuilder();

                foreach (var item in result)
                {
                    //helper.Raw(item.ToString() + "\r\n");
                    //sb.AppendLine(item.ToString());
                    //sw.WriteLine(item.ToString());
                    item.Render(sw, HtmlResource.Cached);
                }

                // remove all resources once rendered...
                rs.Clear();

            });
        }

        private static void Build(List<HtmlResource> src, List<HtmlResource> dest)
        {
            foreach (var item in src)
            {
                Build(item.Dependencies, dest);

                if (dest.Contains(item))
                    continue;

                dest.Add(item);
            }
        }

    }
}

namespace NeuroSpeech.Atoms.Mvc
{

    public abstract class HtmlResource {


        public static bool Cached
        {
            get;
            set;
        }

        private static List<HtmlResource> registeredResources = new List<HtmlResource>();

        private static T Create<T>(string path, params HtmlResource[] dependencies)
            where T:HtmlResource
        {
            lock (registeredResources) {
                if (registeredResources.Any( x=> string.Equals(x.Path, path, StringComparison.CurrentCultureIgnoreCase))) {
                    throw new InvalidOperationException("Resource " + path + " is already registered");
                }
                var rs = Activator.CreateInstance<T>();
                rs.Path = path;
                rs.Dependencies = new List<HtmlResource>();
                if (dependencies != null && dependencies.Length > 0) {
                    rs.Dependencies.AddRange(dependencies);
                }
                registeredResources.Add(rs);
                return rs;
            }

        }

        /// <summary>
        /// Creates Global Script Resource
        /// </summary>
        /// <param name="name"></param>
        /// <param name="path"></param>
        /// <param name="dependencies"></param>
        /// <returns></returns>
        public static HtmlResource CreateScript(string path, params HtmlResource[] dependencies)
        {
            return Create<HtmlScriptResource>(path, dependencies);
        }

        /// <summary>
        /// Creates Global Stylesheet Resource
        /// </summary>
        /// <param name="name"></param>
        /// <param name="path"></param>
        /// <param name="dependencies"></param>
        /// <returns></returns>
        public static HtmlResource CreateStyleSheet(string path, params HtmlResource[] dependencies)
        {
            return Create<HtmlStyleSheetResource>(path, dependencies);
        }

        /// <summary>
        /// Creates inline Script Resource, that will be rendered in the Header
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static HtmlResource CreateInlineScript(string code) {
            var s = new HtmlScriptResource();
            s.Code = code;
            return s;
        }

        /// <summary>
        /// Creates JavaScript Variable with Provided name for Model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static HtmlResource CreateScriptModel(string name, object model) {
            var s = new HtmlScriptResource();
            AtomJavaScriptSerializer js = new AtomJavaScriptSerializer(null);
            s.Code = "var " + name + " = " + js.Serialize(model) + ";";
            return s;
        }

        /// <summary>
        /// Creates inline Stylesheet Resource, that will be rendered in Header
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static HtmlResource CreateStyle(string code)
        {
            var s = new HtmlStyleSheetResource();
            s.Code = code;
            return s;
        }

        internal protected HtmlResource()
        {
            Dependencies = new List<HtmlResource>();
        }

        internal string Path { get; set; }

        internal string Code { get; set; }

        internal List<HtmlResource> Dependencies { get;  set; }

        internal abstract void Render(TextWriter sw, bool cached);
        
    }

    internal class HtmlScriptResource : HtmlResource {

        internal override void Render(TextWriter sw, bool cached)
        {
            sw.WriteLine("<!-- Added by HtmlResource -->");
            if (string.IsNullOrWhiteSpace(Code))
            {
                string src = Path;
                if (cached) {
                    src = CachedRoute.CachedUrl(src).ToHtmlString();
                }
                sw.WriteLine("<script src='{0}' type='text/javascript'></script>", src);
                return;
            }
            sw.WriteLine("<script type='text/javascript'>\r\n\t{0}\r\n</script>\r\n", Code);
        }
    }

    internal class HtmlStyleSheetResource : HtmlResource {
        internal override void Render(TextWriter sw, bool cached)
        {
            sw.WriteLine("<!-- Added by HtmlResource -->");
            if (string.IsNullOrWhiteSpace(Code))
            {
                string src = Path;
                if (cached) {
                    src = CachedRoute.CachedUrl(src).ToHtmlString();
                }
                sw.WriteLine("<link rel='stylesheet' href='{0}'/>", src);
                return;
            }
            sw.WriteLine("<style>\r\n{0}\r\n</style>\r\n", Code);
        }
        //    if (string.IsNullOrWhiteSpace(Code))
        //    {
        //        return string.Format("<link rel='stylesheet' href='{0}'/>", Path);
        //    }
        //    return string.Format("<style>\r\n{0}\r\n</style>\r\n", Path);
        //}
    }



}
