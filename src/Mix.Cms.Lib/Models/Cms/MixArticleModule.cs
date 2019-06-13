﻿using System;
using System.Collections.Generic;

namespace Mix.Cms.Lib.Models.Cms
{
    public partial class MixArticleModule
    {
        public MixArticleModule()
        {
            MixModuleAttributeSet = new HashSet<MixModuleAttributeSet>();
        }

        public int ModuleId { get; set; }
        public int ArticleId { get; set; }
        public string Specificulture { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public int Position { get; set; }
        public int Priority { get; set; }
        public int Status { get; set; }

        public virtual MixArticle MixArticle { get; set; }
        public virtual MixModule MixModule { get; set; }
        public virtual ICollection<MixModuleAttributeSet> MixModuleAttributeSet { get; set; }
    }
}
