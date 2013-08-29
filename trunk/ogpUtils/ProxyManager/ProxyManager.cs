using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Platform = HostMgd;
using Teigha.DatabaseServices;
using Teigha.Runtime;
using platformApp = HostMgd.ApplicationServices.Application;
using platformExeption = Teigha.Runtime.Exception;
using HostMgd.ApplicationServices;
using HostMgd.EditorInput;

namespace ogpUtils
{
    class ProxyManager
    {
        private Editor ed = platformApp.DocumentManager.MdiActiveDocument.Editor;
        private int _explodedcount = 0;
        // http://adndevblog.typepad.com/autocad/2013/04/search-and-erase-proxies.html

        [CommandMethod("OGPproxyKill")]
        public void searchAndExplodeProxy()
        {
            _NODEntriesForErase = new ObjectIdCollection();
            _explodedcount = 0;
            Database db = Application.DocumentManager.
              MdiActiveDocument.Database;
            using (Transaction trans = db.TransactionManager.
              StartTransaction())
            {
                // open block table
                BlockTable bt = trans.GetObject(db.BlockTableId,
                  OpenMode.ForRead) as BlockTable;
                // for each block table record
                // (mspace, pspace, other blocks)
                foreach (ObjectId btrId in bt)
                {
                    BlockTableRecord btr = trans.GetObject(btrId,
                      OpenMode.ForRead) as BlockTableRecord;
                    // for each entity on this block table record
                    foreach (ObjectId entId in btr)
                    {
                        Entity ent = trans.GetObject(entId,
                          OpenMode.ForRead) as Entity;
                        if ((ent.IsAProxy))
                        {
                            try
                            {
                                ent.UpgradeOpen();

                                // Это коллекция объектов, которая будет включать все элементы взорванной прокси
                                DBObjectCollection objs = new DBObjectCollection();

                                // Взрываем блок в нашу коллекцию объектов
                                ent.Explode(objs);

                                // Открываем текущее пространство на запись 
                                btr.UpgradeOpen();

                                // Пробегаем по коллекции объектов и 
                                // каждый из них добавляем к текущему пространству
                                foreach (DBObject obj in objs)
                                {
                                    //преобразуем объект к Entity
                                    Entity entExplode = (Entity)obj;
                                    //Добавляем эту Entity в пространство
                                    btr.AppendEntity(entExplode);
                                    //Добавляем к транзакции новые объекты
                                    trans.AddNewlyCreatedDBObject(entExplode, true);
                                };


                            }
                            catch (platformExeption ex)
                            {
                                //Если что-то сломалось, то в командную строку выводится ошибка
                                ed.WriteMessage("Не могу взорвать - ошибка: " + ex.Message);
                            };

                            try
                            {
                                //и удаляем
                                ent.Erase();
                                _explodedcount++;
                                //ed.WriteMessage(string.Format("\nВзорван прокси в {0}", btr.Name));
                            }
                            catch
                            {
                                ed.WriteMessage("Не могу удалить объект: " + ent.BlockName);
                            }
                        }
                    }
                }

                // now search for NOD proxy entries
                DBDictionary nod = trans.GetObject(
                  db.NamedObjectsDictionaryId,
                  OpenMode.ForRead) as DBDictionary;
                searchSubDictionary(trans, nod);
                trans.Commit();
            }

            // the HandOverTo operation must
            // be perfomed outside transactions
            foreach (ObjectId nodID in _NODEntriesForErase)
            {
                //replace with an empty Dic Entry
                DBDictionary newNODEntry = new DBDictionary();
#pragma warning disable
                DBObject NODEntry = nodID.Open(OpenMode.ForWrite);
#pragma warning enable
                try
                {
                    NODEntry.HandOverTo(newNODEntry, true, true);
                }
                catch { };

                // if we cannot close the OLD entry,
                // then is not database resident
#pragma warning disable
                //http://stackoverflow.com/questions/968293/c-sharp-selectively-suppress-custom-obsolete-warnings
                try { NODEntry.Close(); }
#pragma warining enable
                catch
                {
                    //ed.WriteMessage("\nErased");
                }

                //close the new one
                try { newNODEntry.Close(); }
                catch { }
            }
            ed.WriteMessage("Взорвано Proxy: " + _explodedcount);
        }

        private static ObjectIdCollection
          _NODEntriesForErase;

        private void searchSubDictionary(
          Transaction trans, DBDictionary dic)
        {
            foreach (DBDictionaryEntry dicEntry in dic)
            {
                DBObject subDicObj = trans.GetObject(
                  dicEntry.Value, OpenMode.ForRead);
                if (subDicObj.IsAProxy)
                {
                    ed.WriteMessage(string.Format("\nНайден прокси подобъект: {0}",dicEntry.Key));
                    subDicObj.UpgradeOpen();

                    // for several Proxy Entities the
                    // Erase is not allowed without the
                    // Object Enabler throw an exception,
                    // just treat
                    try
                    {
                        subDicObj.Erase();
                        continue;
                    }
                    catch
                    {
                        ed.WriteMessage("Прокси недоступен для удаления");
                        //try again latter
                        _NODEntriesForErase.Add(subDicObj.ObjectId);
                    }
                }
                else
                {
                    //if this key is not proxy, let go into sub keys
                    DBDictionary subDic =
                      subDicObj as DBDictionary;
                    if (subDic != null)
                        searchSubDictionary(trans, subDic);
                }
            }
        }


        //---------------------------------------------------------------------------------------
        //another try
#if DEBUG
        [CommandMethod("rivilisProxyKill")]
#endif
        public  void rivilisProxyKill()
        {
            Database db = Application.DocumentManager.
              MdiActiveDocument.Database;
            EraseProxies(db);
        }

        private void EraseProxies(Database db)
        {
            RXClass zombieEntity = RXClass.GetClass(typeof(ProxyEntity));
            RXClass zombieObject = RXClass.GetClass(typeof(ProxyObject));
            ObjectId id;
            for (long l = db.BlockTableId.Handle.Value; l < db.Handseed.Value; l++)
            {
                if (!db.TryGetObjectId(new Handle(l), out id))
                    continue;
                if (id.ObjectClass.IsDerivedFrom(zombieObject) && !id.IsErased)
                {
                    try
                    {
                        using (DBObject proxy = id.Open(OpenMode.ForWrite))
                        {
                            proxy.Erase();
                        }
                    }
                    catch
                    {
                        using (DBDictionary newDict = new DBDictionary())
                        using (DBObject proxy = id.Open(OpenMode.ForWrite))
                        {
                            try
                            {
                                proxy.HandOverTo(newDict, true, true);
                            }
                            catch { }
                        }
                    }
                }
                else if (id.ObjectClass.IsDerivedFrom(zombieEntity) && !id.IsErased)
                {
                    try
                    {
                        using (DBObject proxy = id.Open(OpenMode.ForWrite))
                        {
                            proxy.Erase();
                        }
                    }
                    catch { }
                }
            }
        }

        /*
        //Другой пример удаления проксей
                // http://adndevblog.typepad.com/autocad/2012/07/remove-proxy-objects-from-database.html
        
                public void RemoveEntry1(
                  DBDictionary dict, ObjectId id, Transaction tr)
                {
                    ProxyObject obj =
                      (ProxyObject)tr.GetObject(id, OpenMode.ForRead);

                    // If you want to check what exact proxy it is
                    if (obj.OriginalClassName != "ProxyToRemove")
                        return;

                    dict.Remove(id);
                }

                public void RemoveEntry2(
                  DBDictionary dict, ObjectId id, Transaction tr)
                {
                    ProxyObject obj =
                      (ProxyObject)tr.GetObject(id, OpenMode.ForRead);

                    // If you want to check what exact proxy it is
                    if (obj.OriginalClassName != "ProxyToRemove")
                        return;

                    obj.UpgradeOpen();

                    using (DBObject newObj = new Xrecord())
                    {
                        obj.HandOverTo(newObj, false, false);
                        newObj.Erase();
                    }
                }

                public void RemoveProxiesFromDictionary(
                  ObjectId dictId, Transaction tr)
                {
                    using (ObjectIdCollection ids = new ObjectIdCollection())
                    {
                        DBDictionary dict =
                          (DBDictionary)tr.GetObject(dictId, OpenMode.ForRead);

                        foreach (DBDictionaryEntry entry in dict)
                        {
                            RXClass c1 = entry.Value.ObjectClass;
                            RXClass c2 = RXClass.GetClass(typeof(ProxyObject));

                            if (entry.Value.ObjectClass.Name == "AcDbZombieObject")
                                ids.Add(entry.Value);
                            else if (entry.Value.ObjectClass ==
                              RXClass.GetClass(typeof(DBDictionary)))
                                RemoveProxiesFromDictionary(entry.Value, tr);
                        }

                        if (ids.Count > 0)
                        {
                            dict.UpgradeOpen();

                            foreach (ObjectId id in ids)
                                RemoveEntry2(dict, id, tr);
                        }
                    }
                }

                [CommandMethod("RemoveProxiesFromNOD", "RemoveProxiesFromNOD",
                  CommandFlags.Modal)]
                public void RemoveProxiesFromNOD()
                {
                    Database db = HostApplicationServices.WorkingDatabase;

                    // Help file says the following about HandOverTo:
                    // "This method is not allowed on objects that are
                    // transaction resident.
                    // If the object on which the method is called is transaction
                    // resident, then no handOverTo operation is performed."
                    // That's why we need to use Open/Close transaction
                    // instead of the normal one
            
                     Старый вариант
                    using (Transaction tr =
                      db.TransactionManager.StartOpenCloseTransaction())
                    {
                        RemoveProxiesFromDictionary(db.NamedObjectsDictionaryId, tr);

                        tr.Commit();
                    }
            

                }

                [CommandMethod("RemoveProxiesFromBlocks", "RemoveProxiesFromBlocks",
                  CommandFlags.Modal)]
                public void RemoveProxiesFromBlocks()
                {
                    Database db = HostApplicationServices.WorkingDatabase;

                    using (Transaction tr =
                      db.TransactionManager.StartTransaction())
                    {
                        BlockTable bt =
                          (BlockTable)tr.GetObject(
                            db.BlockTableId, OpenMode.ForRead);

                        foreach (ObjectId btrId in bt)
                        {
                            BlockTableRecord btr =
                              (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);

                            foreach (ObjectId entId in btr)
                            {
                                if (entId.ObjectClass.Name == "AcDbZombieEntity")
                                {
                                    ProxyEntity ent =
                                      (ProxyEntity)tr.GetObject(entId, OpenMode.ForRead);

                                    // If you want to check what exact proxy it is
                                    if (ent.ApplicationDescription != "ProxyToRemove")
                                        return;

                                    ent.UpgradeOpen();

                                    using (DBObject newEnt = new Line())
                                    {
                                        ent.HandOverTo(newEnt, false, false);
                                        newEnt.Erase();
                                    }
                                }
                            }
                        }

                        tr.Commit();
                    }
                }
         */
    }
}

