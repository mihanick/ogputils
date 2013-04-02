using ogpUtils;

namespace BlockUtils
{
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

//Классы Teigha
using Teigha.Runtime; // Не работает вместе c Multicad.Runtime
using HostMgd.EditorInput;
using Platform = HostMgd;
using PlatformDb = Teigha;
using HostMgd.ApplicationServices;
using Teigha.DatabaseServices;
using Teigha.Geometry;

using Multicad;
using Multicad.DatabaseServices;
using Multicad.DatabaseServices.StandardObjects;
using Multicad.Geometry;
//using Multicad.Runtime;
using Multicad.AplicationServices;

    //Использование определенных типов, которые определены и в платформе и в мультикаде
    public struct BlockProps
    {
        public ObjectId BlockId;
        public string BlockName;
        public bool Explodable;
        public BlockScaling UniformScale;
    }



    class DrawingBlocksCollection 
    {
        Database AcDb = Platform.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database;
        Document doc = Platform.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
        Editor ed = Platform.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;

        //Внутренняя переменная, задающая список блоков
        List<BlockProps> _blocklist;
        
        //Свойство класса - список блоков
        public  List<BlockProps> ListOfBlocks
        {
            get { return this._blocklist; }
	        set {
                    foreach (BlockProps OneBlock in value)
                        SetBlockProperties(OneBlock);
                    this._blocklist = GetBlockProperties();
	            }
        }

        //Конструктор класса
        public DrawingBlocksCollection()
        {
            this._blocklist = GetBlockProperties();
        }


        public List<BlockProps> GetBlockProperties()
        {
            //Функция возвращает список блоков с их атрибутами

            //Запускаем транзакцию
            Transaction tx = AcDb.TransactionManager.StartTransaction();

            //Находим таблицу описаний блоков 
            BlockTable blkTbl = tx.GetObject(AcDb.BlockTableId, OpenMode.ForRead, false, true) as BlockTable;

            //Открываем таблицу записей текущего чертежа
            BlockTableRecord bt =
                (BlockTableRecord)tx.GetObject(
                AcDb.CurrentSpaceId,
                OpenMode.ForRead
                );
                
            //Переменная списка блоков
            List<BlockProps> bNames = new List<BlockProps>();

            //Пример итерации по таблице определений блоков
            //https://sites.google.com/site/bushmansnetlaboratory/sendbox/stati/multipleattsync
            //Как я понимаю, здесь пробегается по всем таблицам записей,
            //в которых определения блоков не являются анонимными
            //и не являются листами
            foreach (BlockTableRecord btr in blkTbl.Cast<ObjectId>().Select(n =>
                (BlockTableRecord) tx.GetObject(n, OpenMode.ForRead, false))
                .Where(n => !n.IsAnonymous && !n.IsLayout)) 
                {
                    BlockProps bp = new BlockProps();

                    bp.BlockId = btr.ObjectId;
                    bp.BlockName = btr.Name;
                    bp.Explodable=btr.Explodable;
                    bp.UniformScale=btr.BlockScaling;
                    bNames.Add(bp);

                    btr.Dispose();
                };
            tx.Commit();
                                
            return bNames;
        }

        public void SetBlockProperties(BlockProps OneBlock)
        {
            //Функция устанавливает свойства одного блока, которые передаются в виде аргумента

            {
                //ed.WriteMessage(String.Format("DEBUG: Changing {0} - {1} with E={2} and Uscale={3}",OneBlock.BlockId.ToString(), OneBlock.BlockName, OneBlock.Explodable, OneBlock.UniformScale.ToString()));

                //Начинаем транзакцию
                Transaction tx = AcDb.TransactionManager.StartTransaction();

                //Открываем таблицу описаний блоков на запись
                BlockTable blkTbl = tx.GetObject(AcDb.BlockTableId, OpenMode.ForWrite, false, true) as BlockTable;

                if (blkTbl.Has(OneBlock.BlockId)) //Если 
                {
                    try
                    {
                        //Открываем текущий документ на запись
                        BlockTableRecord bt =
                            (BlockTableRecord)tx.GetObject(
                            AcDb.CurrentSpaceId,
                            OpenMode.ForWrite
                            );

                        //Пример итерации по таблице определений блоков
                        //https://sites.google.com/site/bushmansnetlaboratory/sendbox/stati/multipleattsync
                        //Как я понимаю, здесь пробегается по всем таблицам записей,
                        //в которых определения блоков не являются анонимными
                        //и не являются листами
                        foreach (BlockTableRecord btr in blkTbl.Cast<ObjectId>().Select(n =>
                            (BlockTableRecord)tx.GetObject(n, OpenMode.ForWrite, false))
                                .Where(n => !n.IsAnonymous && !n.IsLayout))
                            if (btr.ObjectId == OneBlock.BlockId)  //Если нашли в таблице записей блок с нужным ObjectId
                            {
                                //То назначаем ему атрибуты
                                btr.Name = OneBlock.BlockName;
                                btr.Explodable = OneBlock.Explodable;
                                btr.BlockScaling = OneBlock.UniformScale;

                                ed.WriteMessage(String.Format("Изменен блок {1}", OneBlock.BlockId.ToString(), OneBlock.BlockName, OneBlock.Explodable, OneBlock.UniformScale.ToString()));

                                btr.Dispose();
                            };

                        //Дальше необходимо "уведомить" все вставки блоков об изменении описания блока - пробегаем по чертежу
                        ObjectIdCollection brefIds = bt.GetBlockReferenceIds(true, true);
                        foreach (ObjectId id in brefIds)
                        {
                            BlockReference bl = tx.GetObject(id, OpenMode.ForWrite) as BlockReference;
                            bl.RecordGraphicsModified(true);
                        }
                    }
                    catch (PlatformDb.Runtime.Exception ex)
                    {
                        //Если что-то сломалось, то в командную строку выводится ошибка
                        ed.WriteMessage("Ошибка применения свойств: " + ex.Message);
                    };
                }
                tx.Commit();
            }
        }

    }
}
