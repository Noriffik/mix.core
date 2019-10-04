﻿using Microsoft.EntityFrameworkCore.Storage;
using Mix.Cms.Lib.Models.Cms;
using Mix.Domain.Core.Models;
using Mix.Domain.Core.ViewModels;
using Mix.Domain.Data.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mix.Cms.Lib.ViewModels.MixAttributeSetDatas
{
    public class ODataMobileViewModel
      : ODataViewModelBase<MixCmsContext, MixAttributeSetData, ODataMobileViewModel>
    {
        #region Properties
        #region Models
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("attributeSetId")]
        public int AttributeSetId { get; set; }
        [JsonProperty("attributeSetName")]
        public string AttributeSetName { get; set; }
        [JsonProperty("createdDateTime")]
        public DateTime CreatedDateTime { get; set; }
        [JsonProperty("status")]
        public int Status { get; set; }
        #endregion Models
        #region Views

        [JsonIgnore]
        public List<MixAttributeSetValues.ODataMobileViewModel> Values { get; set; }
        [JsonIgnore]
        public List<MixAttributeFields.ODataMobileViewModel> Fields { get; set; }
        //[JsonIgnore]
        public List<MixAttributeSetDatas.ODataMobileViewModel> RefData { get; set; }= new List<ODataMobileViewModel>();
        [JsonProperty("data")]
        public JObject Data { get; set; }

        [JsonProperty("relatedData")]
        public List<MixRelatedAttributeDatas.ODataMobileViewModel> RelatedData { get; set; } = new List<MixRelatedAttributeDatas.ODataMobileViewModel>();

        #endregion


        #endregion Properties

        #region Contructors

        public ODataMobileViewModel() : base()
        {
        }

        public ODataMobileViewModel(MixAttributeSetData model, MixCmsContext _context = null, IDbContextTransaction _transaction = null) : base(model, _context, _transaction)
        {
        }

        #endregion Contructors

        #region Overrides

        public override void ExpandView(MixCmsContext _context = null, IDbContextTransaction _transaction = null)
        {
            Data = new JObject();
            Data.Add(new JProperty("id", Id));
            Data.Add(new JProperty("details", $"/api/v1/odata/{Specificulture}/attribute-set-data/mobile/{Id}"));
            Values = MixAttributeSetValues.ODataMobileViewModel
                .Repository.GetModelListBy(a => a.DataId == Id && a.Specificulture == Specificulture, _context, _transaction).Data.OrderBy(a => a.Priority).ToList();
            foreach (var item in Values.OrderBy(v=>v.Priority))
            {
                Data.Add(ParseValue(item));
            }
        }
        public override MixAttributeSetData ParseModel(MixCmsContext _context = null, IDbContextTransaction _transaction = null)
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = Guid.NewGuid().ToString();
                CreatedDateTime = DateTime.UtcNow;
                Priority = Repository.Count(m => m.AttributeSetName == AttributeSetName && m.Specificulture == Specificulture,_context,_transaction).Data + 1;
            }
            Values = Values ?? MixAttributeSetValues.ODataMobileViewModel
                .Repository.GetModelListBy(a => a.DataId == Id && a.Specificulture == Specificulture, _context, _transaction).Data.OrderBy(a => a.Priority).ToList();
            Fields = MixAttributeFields.ODataMobileViewModel.Repository.GetModelListBy(f => f.AttributeSetId == AttributeSetId, _context, _transaction).Data;
            if (string.IsNullOrEmpty(AttributeSetName))
            {
                AttributeSetName = _context.MixAttributeSet.First(m => m.Id == AttributeSetId)?.Name;
            }
            foreach (var field in Fields.OrderBy(f => f.Priority))
            {
                var val = Values.FirstOrDefault(v => v.AttributeFieldId == field.Id);
                if (val == null)
                {
                    val = new MixAttributeSetValues.ODataMobileViewModel(
                        new MixAttributeSetValue() {
                            AttributeFieldId = field.Id,
                            AttributeFieldName = field.Name,                            
                        }
                        , _context, _transaction);
                    val.Priority = field.Priority;
                    Values.Add(val);
                }
                val.Priority = field.Priority;
                val.AttributeSetName = AttributeSetName;
                if (Data[val.AttributeFieldName] != null)
                {
                    if (val.Field.DataType != MixEnums.MixDataType.Reference)
                    {
                        ParseModelValue(Data[val.AttributeFieldName], val);
                    }
                    else
                    {
                        var arr = Data[val.AttributeFieldName].Value<JArray>();
                        foreach (JObject objData in arr)
                        {
                            RefData.Add(new ODataMobileViewModel()
                            {
                                AttributeSetId = field.ReferenceId.Value,
                                Data = objData["data"].Value<JObject>()
                            });
                        }
                    }
                    
                }
                else
                {
                    Data.Add(ParseValue(val));
                }
            }
                
            return base.ParseModel(_context, _transaction); ;
        }

        #region Async
        public override async Task<RepositoryResponse<bool>> SaveSubModelsAsync(MixAttributeSetData parent, MixCmsContext _context, IDbContextTransaction _transaction)
        {
            var result = new RepositoryResponse<bool>() { IsSucceed = true };
            if (result.IsSucceed)
            {
                RepositoryResponse<bool> saveValues = await SaveValues(parent, _context, _transaction);
            }
            // Save Ref Data
            if (result.IsSucceed)
            {
                RepositoryResponse<bool> saveRefData = await SaveRefDataAsync(parent, _context, _transaction);
                ViewModelHelper.HandleResult(saveRefData, ref result);
            }
            
            // Save Related Data
            if (result.IsSucceed)
            {
                RepositoryResponse<bool> saveRelated = await SaveRelatedDataAsync(parent, _context, _transaction);
                ViewModelHelper.HandleResult(saveRelated, ref result);
            }
            
            return result;
        }

        private async Task<RepositoryResponse<bool>> SaveValues(MixAttributeSetData parent, MixCmsContext context, IDbContextTransaction transaction)
        {
            var result = new RepositoryResponse<bool>() { IsSucceed = true };
            foreach (var item in Values)
            {
                if (result.IsSucceed)
                {
                    if (Fields.Any(f => f.Id == item.AttributeFieldId))
                    {
                        item.Priority = item.Field.Priority;
                        item.DataId = parent.Id;
                        item.Specificulture = parent.Specificulture;
                        var saveResult = await item.SaveModelAsync(false, context, transaction);
                        ViewModelHelper.HandleResult(saveResult, ref result);
                    }
                    else
                    {
                        var delResult = await item.RemoveModelAsync(false, context, transaction);
                        ViewModelHelper.HandleResult(delResult, ref result);
                    }
                }
                else
                {
                    break;
                }
            }
            return result;
        }

        private async Task<RepositoryResponse<bool>> SaveRefDataAsync(MixAttributeSetData parent, MixCmsContext context, IDbContextTransaction transaction)
        {
            var result = new RepositoryResponse<bool>() { IsSucceed = true };
            foreach (var item in RefData)
            {
                if (result.IsSucceed)
                {
                    item.Specificulture = Specificulture;
                    var saveRef = await item.SaveModelAsync(true, context, transaction);
                    if (saveRef.IsSucceed)
                    {
                        RelatedData.Add(new MixRelatedAttributeDatas.ODataMobileViewModel()
                        {
                            Id = saveRef.Data.Id,
                            ParentId = Id,                            
                            ParentType = MixEnums.MixAttributeSetDataType.Set,
                            AttributeSetId = saveRef.Data.AttributeSetId,
                            AttributeSetName = saveRef.Data.AttributeSetName,
                            CreatedDateTime = DateTime.UtcNow,
                            Specificulture = Specificulture
                        });
                    }
                    ViewModelHelper.HandleResult(saveRef, ref result);
                }
                else
                {
                    break;
                }
                
            }
            return result;
        }

        private async Task<RepositoryResponse<bool>> SaveRelatedDataAsync(MixAttributeSetData parent, MixCmsContext context, IDbContextTransaction transaction)
        {
            var result = new RepositoryResponse<bool>() { IsSucceed = true };

            foreach (var item in RelatedData)
            {
                if (result.IsSucceed)
                {
                    if (string.IsNullOrEmpty(item.ParentId) && item.ParentType == MixEnums.MixAttributeSetDataType.Set)
                    {
                        var set = context.MixAttributeSet.First(s => s.Name == item.ParentName);
                        item.ParentId = set.Id.ToString();
                    }
                    if (!string.IsNullOrEmpty(item.ParentId))
                    {

                        item.Specificulture = Specificulture;
                        item.AttributeSetId = parent.AttributeSetId;
                        item.AttributeSetName = parent.AttributeSetName;
                        item.Id = string.IsNullOrEmpty(item.Id) ? parent.Id : item.Id;
                        item.CreatedDateTime = DateTime.UtcNow;
                        var saveResult = await item.SaveModelAsync(true, context, transaction);
                        ViewModelHelper.HandleResult(saveResult, ref result);
                    }
                    else
                    {
                        result.IsSucceed = false;
                        result.Errors.Add("Invalid Parent");
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            return result;
        }
        #endregion
        #endregion

        #region Expands
        JProperty ParseValue(MixAttributeSetValues.ODataMobileViewModel item)
        {
            switch (item.DataType)
            {
                case MixEnums.MixDataType.DateTime:
                    return new JProperty(item.AttributeFieldName, item.DateTimeValue);
                case MixEnums.MixDataType.Date:
                    return (new JProperty(item.AttributeFieldName, item.DateTimeValue));
                case MixEnums.MixDataType.Time:
                    return (new JProperty(item.AttributeFieldName, item.DateTimeValue));
                case MixEnums.MixDataType.Double:
                    return (new JProperty(item.AttributeFieldName, item.DoubleValue));
                case MixEnums.MixDataType.Boolean:
                    return (new JProperty(item.AttributeFieldName, item.BooleanValue));
                case MixEnums.MixDataType.Number:
                    return (new JProperty(item.AttributeFieldName, item.IntegerValue));
                case MixEnums.MixDataType.Reference:
                    //string url = $"/api/v1/odata/en-us/related-attribute-set-data/mobile/parent/set/{Id}/{item.Field.ReferenceId}";
                    return (new JProperty(item.AttributeFieldName, new JArray()));
                case MixEnums.MixDataType.Custom:
                case MixEnums.MixDataType.Duration:
                case MixEnums.MixDataType.PhoneNumber:
                case MixEnums.MixDataType.Text:
                case MixEnums.MixDataType.Html:
                case MixEnums.MixDataType.MultilineText:
                case MixEnums.MixDataType.EmailAddress:
                case MixEnums.MixDataType.Password:
                case MixEnums.MixDataType.Url:
                case MixEnums.MixDataType.ImageUrl:
                case MixEnums.MixDataType.CreditCard:
                case MixEnums.MixDataType.PostalCode:
                case MixEnums.MixDataType.Upload:
                case MixEnums.MixDataType.Color:
                case MixEnums.MixDataType.Icon:
                case MixEnums.MixDataType.VideoYoutube:
                case MixEnums.MixDataType.TuiEditor:
                default:
                    return (new JProperty(item.AttributeFieldName, item.StringValue));
            }
        }
        void ParseModelValue(JToken property, MixAttributeSetValues.ODataMobileViewModel item)
        {
            switch (item.DataType)
            {
                case MixEnums.MixDataType.DateTime:
                    item.DateTimeValue = property.Value<DateTime?>();
                    break;
                case MixEnums.MixDataType.Date:
                    item.DateTimeValue = property.Value<DateTime?>();
                    break;                    
                case MixEnums.MixDataType.Time:
                    item.DateTimeValue = property.Value<DateTime?>();
                    break;
                case MixEnums.MixDataType.Double:
                    item.DoubleValue = property.Value<double?>();
                    break;
                case MixEnums.MixDataType.Boolean:
                    item.BooleanValue = property.Value<bool?>();
                    break;
                case MixEnums.MixDataType.Number:
                    item.IntegerValue = property.Value<int?>();
                    break;
                case MixEnums.MixDataType.Reference:
                   
                    break;
                case MixEnums.MixDataType.Custom:
                case MixEnums.MixDataType.Duration:
                case MixEnums.MixDataType.PhoneNumber:
                case MixEnums.MixDataType.Text:
                case MixEnums.MixDataType.Html:
                case MixEnums.MixDataType.MultilineText:
                case MixEnums.MixDataType.EmailAddress:
                case MixEnums.MixDataType.Password:
                case MixEnums.MixDataType.Url:
                case MixEnums.MixDataType.ImageUrl:
                case MixEnums.MixDataType.CreditCard:
                case MixEnums.MixDataType.PostalCode:
                case MixEnums.MixDataType.Upload:
                case MixEnums.MixDataType.Color:
                case MixEnums.MixDataType.Icon:
                case MixEnums.MixDataType.VideoYoutube:
                case MixEnums.MixDataType.TuiEditor:
                default:
                    item.StringValue = property.Value<string>();
                    break;
            }
            item.StringValue = property.Value<string>();
        }
        #endregion
    }
}