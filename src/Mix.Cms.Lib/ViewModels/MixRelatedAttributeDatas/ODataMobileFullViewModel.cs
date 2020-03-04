﻿using Microsoft.EntityFrameworkCore.Storage;
using Mix.Cms.Lib.Models.Cms;
using Mix.Domain.Data.ViewModels;
using Newtonsoft.Json;
using System;

namespace Mix.Cms.Lib.ViewModels.MixRelatedAttributeDatas
{
    public class ODataMobileFullViewModel
       : ODataViewModelBase<MixCmsContext, MixRelatedAttributeData, ODataMobileFullViewModel>
    {
        #region Model

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("parentId")]
        public string ParentId { get; set; }

        [JsonProperty("parentType")]
        public MixEnums.MixAttributeSetDataType ParentType { get; set; }

        [JsonProperty("attributeSetId")]
        public int AttributeSetId { get; set; }

        [JsonProperty("attributeSetName")]
        public string AttributeSetName { get; set; }

        [JsonProperty("createdDateTime")]
        public DateTime CreatedDateTime { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        #endregion Model

        #region Views

        [JsonProperty("parentName")]
        public string ParentName { get; set; }

        [JsonProperty("data")]
        public MixAttributeSetDatas.ODataMobileFullViewModel Data { get; set; }

        #endregion Views

        public ODataMobileFullViewModel(MixRelatedAttributeData model, MixCmsContext _context = null, IDbContextTransaction _transaction = null)
            : base(model, _context, _transaction)
        {
        }

        public ODataMobileFullViewModel() : base()
        {
        }

        #region overrides

        public override void ExpandView(MixCmsContext _context = null, IDbContextTransaction _transaction = null)
        {
            var getData = MixAttributeSetDatas.ODataMobileFullViewModel.Repository.GetSingleModel(p => p.Id == Id && p.Specificulture == Specificulture
                , _context: _context, _transaction: _transaction
            );
            if (getData.IsSucceed)
            {
                Data = getData.Data;
            }
        }

        #endregion overrides
    }
}