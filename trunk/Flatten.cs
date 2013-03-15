namespace MultiDotNet
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


    namespace TeighaPlatform
    {
        //Использование определенных типов, которые определены и в платформе и в мультикаде
        using Hatch = Teigha.DatabaseServices.Hatch;
        using Point3d = Teigha.Geometry.Point3d;
        using Vector3d = Teigha.Geometry.Vector3d;
        using Polyline3d = Teigha.DatabaseServices.Polyline3d;

        class nanoFlattener
        {

            //Замечания:
            //BUG: Не могу обработать тип объекта: AcDbSolid
            //BUG: Не могу обработать тип объекта: AcDbRegion
            //BUG: Не могу обработать тип объекта: AcDbSurface
            //BUG: Не могу обработать тип объекта: AcDbPolygonMesh
            //BUG: Не могу обработать тип объекта: AcDbPolygonMesh


            //BUG: Неправильно преобразуется штриховка
            //BUG: Окружности не проецируются в эллипсы

            //Общие объекты всего класса
            Database acCurDb = Platform.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database;
            Document acCurDoc = Platform.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = Platform.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;

            //Счетчики количества обработанных объектов по каждому типу
            private int txtcount;
            private int mtxtcount;
            private int dimcount;
            private int linecount;
            private int plinecount;
            private int arccount;
            private int circlecount;
            private int splinecount;
            private int hatchcount;
            private int pointcount;
            private int blockmoved;
            private int blockexploded;
            private int noflatcount;
            private int ellipsecount;
            private int attributedefcount;
            private int leadercount;


            //Переменная, которая задает взрывать ли блоки, которые нужно расплющить
            public bool toExplodeBlocks = true;

            //Регистрируем команду multiFlatten (средствами платформы
            //CommandFlags.UsePickSet - добавляет возможность выбрать объекты до запуска команды.
            [CommandMethod("OGPmultiFlatten", CommandFlags.UsePickSet)]
            public void multiFlatten()
            {
                //Опции функции выбора
                PromptSelectionOptions pso = new PromptSelectionOptions();

                //Строчки контекстного меню
                string strEraseBlocks = "Взрывать";
                string strNoEraseBlocks = "Оставить";

                pso.AllowDuplicates = false;
                pso.AllowSubSelections = true;
                pso.RejectObjectsFromNonCurrentSpace = true;
                pso.RejectObjectsOnLockedLayers = false;

                //Добавляем выбор в контекстное меню
                pso.Keywords.Add(strEraseBlocks);
                pso.Keywords.Add(strNoEraseBlocks);

                //Назначаем дефолтную опцию контекстного меню и приглашение
                if (toExplodeBlocks)
                {
                    pso.MessageForAdding = "Выберите объекты (" + strEraseBlocks + " блоки и прокси): ";
                    pso.Keywords.Default = strEraseBlocks;
                }
                else
                {
                    pso.MessageForAdding = "Выберите объекты (" + strNoEraseBlocks + " блоки и прокси): ";
                    pso.Keywords.Default = strNoEraseBlocks;
                };

                /*
                //http://through-the-interface.typepad.com/through_the_interface/2010/05/adding-keyword-handling-to-autocad-nets-getselection.html#sthash.WiNnKYvG.dpuf
                */

                //Запускаем выбор объектов
                PromptSelectionResult sr = ed.GetSelection(pso);

                //Если не объект выбран, а опция контекстного меню, то toExplodeBlocks в зависимости от выбора опций пользователем
                if (sr.StringResult == strEraseBlocks) toExplodeBlocks = true;
                if (sr.StringResult == strNoEraseBlocks) toExplodeBlocks = false;

                //Если выбор осуществлен, то запускаем функцию
                if (sr.Status == PromptStatus.OK)
                {
                    //Обнуляем счетчики
                    txtcount = 0;
                    mtxtcount = 0;
                    dimcount = 0;
                    linecount = 0;
                    plinecount = 0;
                    arccount = 0;
                    circlecount = 0;
                    splinecount = 0;
                    hatchcount = 0;
                    pointcount = 0;
                    blockmoved = 0;
                    blockexploded = 0;
                    noflatcount = 0;
                    ellipsecount = 0;
                    attributedefcount = 0;
                    leadercount = 0;


                    //Массив выбранных объектов
                    ObjectId[] objIds = sr.Value.GetObjectIds();

                    //Пробегаем по всем объектам
                    int i = 1;
                    foreach (ObjectId asObjId in objIds)
                    {
                        //ed.WriteMessage("DEBUG: Обрабатывается объект "+i+" из "+objIds.Length);

                        //Плющим каждый объект по одному
                        FlattenByPlatform(asObjId, toExplodeBlocks);

                        //Счетчик тут для того чтобы в дебаге можно было вывести прогресс
                        i++;
                    }

                    //Выводим результаты обработки - 
                    //по каждому из типов объектов, а также необработанные
                    ed.WriteMessage("Всего объектов: " + objIds.Length);
                    if (txtcount != 0) ed.WriteMessage("Обработано " + txtcount + " однострочных текстов");
                    if (mtxtcount != 0) ed.WriteMessage("Обработано " + mtxtcount + " Мтекстов");
                    if (dimcount != 0) ed.WriteMessage("Обработано " + dimcount + " размеров");
                    if (linecount != 0) ed.WriteMessage("Обработано " + linecount + " линий");
                    if (plinecount != 0) ed.WriteMessage("Обработано " + plinecount + " полилиний");
                    if (arccount != 0) ed.WriteMessage("Обработано " + arccount + " дуг");
                    if (circlecount != 0) ed.WriteMessage("Обработано " + circlecount + " окружностей");
                    if (splinecount != 0) ed.WriteMessage("Обработано " + splinecount + " сплайнов");
                    if (hatchcount != 0) ed.WriteMessage("Обработано " + hatchcount + " штриховок");
                    if (pointcount != 0) ed.WriteMessage("Обработано " + pointcount + " точек");
                    if (attributedefcount != 0) ed.WriteMessage("Обработано " + attributedefcount + " точек");
                    if (leadercount != 0) ed.WriteMessage("Обработано " + leadercount + " точек");
                    if (blockmoved != 0) ed.WriteMessage("Перемещено " + blockmoved + " блоков");
                    if (blockexploded != 0) ed.WriteMessage("Взорвано " + blockexploded + " блоков");
                    if (noflatcount != 0) ed.WriteMessage("Не обработано " + noflatcount + " объектов");

                }
            }


            public bool FlattenByPlatform(ObjectId id_platf, bool explodeBlocks)
            {
                /*
                Функция расплющивания всех типов примитивов.
                Возвращает true если скормленный ей объект был расплющен
                и возвращает false если плющить было нечего.
                принимает на входе ObjectId того объекта, который нужно расплющить
                и переменную explodeBlocks которая задает взрывать ли блоки или нет.
                */


                bool result = false; //Пока ничего не плющили результат равен false

                try
                {//Всякое может случиться

                    //Запускаем транзакцию
                    Transaction transaction = acCurDb.TransactionManager.StartTransaction();
                    //Открываем переданный в функцию объект на чтение, преобразуем его к Entity
                    Entity ent = (Entity)transaction.GetObject(id_platf, OpenMode.ForWrite);

                    //Далее последовательно проверяем класс объекта на соответствие классам основных
                    //примитивов

                    if (id_platf.ObjectClass.Name == "AcDbLine")
                    {//Если объект - отрезок (line)
                        Line kline = ent as Line; //Преобразуем к типу линия

                        if (kline.StartPoint.Z != 0 || kline.EndPoint.Z != 0 || kline.Thickness!=0) //если начальная или конечная точка не лежит на ХоУ
                        {

                            //То зануляем координату Z начала или конца
                            kline.StartPoint = new Teigha.Geometry.Point3d(kline.StartPoint.X, kline.StartPoint.Y, 0);
                            kline.EndPoint = new Teigha.Geometry.Point3d(kline.EndPoint.X, kline.EndPoint.Y, 0);

                            kline.Thickness = 0;

                            //ed.WriteMessage("DEBUG: Преобразован объект: линия");

                            //Возвращаем результат
                            result = true;
                            //Увеличиваем количество обработанных линий
                            linecount++;
                        };

                    }
                    else if (id_platf.ObjectClass.Name == "AcDbBlockReference")
                    {//Блок
                        BlockReference blk = ent as BlockReference;

                        //Если блок просто размещен в пронстранстве (не на XoY)
                        if (blk.Position.Z != 0)
                        {
                            //То просто зануляем ему координату Z
                            blk.Position = new Point3d(blk.Position.X, blk.Position.Y, 0);
                            //ed.WriteMessage("DEBUG: Преобразован объект: блок(перемещен)");

                            //Увеличиваем число перемещенных блоков
                            blockmoved++;
                            result = true;
                        };
                        //Если найден блок - его разбираем, и если в нем есть неплоские объекты - плющим
                        if (ExplodeBlock(id_platf, explodeBlocks))
                        {
                            //ed.WriteMessage("DEBUG: Преобразован объект: блок (взорван)");

                            //Для отчетности увеличиваем счетчик взорванных блоков
                            blockexploded++;
                            result = true;
                        }
                    }
                    else if (id_platf.ObjectClass.Name == "AcDbProxyEntity")
                    {//Блок
                        ProxyEntity pxy = ent as ProxyEntity;

                        //Если найден Прокси - его разбираем, и если в нем есть неплоские объекты - плющим
                        if (ExplodeProxy(id_platf, explodeBlocks))
                        {
                            //ed.WriteMessage("DEBUG: Преобразован объект: блок (взорван)");

                            //Для отчетности увеличиваем счетчик взорванных блоков
                            blockexploded++;
                            result = true;
                        }
                    }
                    else if (id_platf.ObjectClass.Name == "AcDbPolyline")
                    {//Если объект - полилиния
                        Polyline kpLine = (Polyline)ent;

                        //Если у полинии Elevation не 0 или полилинию можно расплющить (там идет проверка по каждой вершине)
                        if (kpLine.Elevation != 0 || kpLine.Thickness != 0 || flattenPolyline(ent))
                        {
                            kpLine.Thickness = 0;
                            kpLine.Elevation = 0;

                            //ed.WriteMessage("DEBUG: Преобразован объект: полилиния");
                            result = true;
                            plinecount++;
                        };
                    }
                    else if (id_platf.ObjectClass.Name == "AcDb3dPolyline")
                    {//2D полилиния - такие тоже попадаются
                        Polyline3d kpLine = (Polyline3d)ent;

                        if (flattenPolyline(ent))
                        {
                            //ed.WriteMessage("DEBUG: Преобразован объект: 3d полилиния");
                            result = true;
                            plinecount++;
                        };
                    }
                    else if (id_platf.ObjectClass.Name == "AcDb2dPolyline")
                    {//2D полилиния - такие тоже попадаются
                        Polyline2d kpLine = (Polyline2d)ent;

                        if (kpLine.Elevation != 0 ||kpLine.Thickness!=0||flattenPolyline(ent))
                        {
                            kpLine.Thickness = 0;
                            kpLine.Elevation = 0;

                            //ed.WriteMessage("DEBUG: Преобразован объект: 2d полилиния");
                            result = true;
                            plinecount++;
                        };
                    }
                    else if (id_platf.ObjectClass.Name == "AcDbCircle")
                    {//Окружность
                        Circle cir = (Circle)ent;
                        if (cir.Center.Z != 0 || cir.StartPoint.Z != 0 || cir.EndPoint.Z != 0 || cir.Thickness!=0)
                        {
                            cir.Center = new Teigha.Geometry.Point3d(cir.Center.X, cir.Center.Y, 0);
                            cir.Thickness = 0;
                            //TODO: Проецирование окружностей в эллипс

                            //ed.WriteMessage("DEBUG: Преобразован объект: окружность");
                            result = true;
                            circlecount++;
                        }
                    }
                    else if (id_platf.ObjectClass.Name == "AcDbArc")
                    {//Дуга
                        Arc arc = ent as Arc;

                        //if (arc.Center.Z != 0 || arc.StartPoint.Z != 0 || arc.EndPoint.Z != 0)
                        if (arc.Center.Z != 0 || arc.Thickness!=0)
                        {
                            arc.Center = new Point3d(arc.Center.X, arc.Center.Y, 0);
                            arc.Thickness = 0;
                            //BUG: StartPoint и EndPoint дуги по-моему только READ-ONLY
                            //arc.StartPoint = new Point3d(arc.StartPoint.X, arc.StartPoint.Y, 0);
                            //arc.EndPoint = new Point3d(arc.EndPoint.X, arc.EndPoint.Y, 0);

                            //ed.WriteMessage("DEBUG: Преобразован объект: Дуга");
                            result = true;
                            arccount++;
                        }
                    }
                    else if (id_platf.ObjectClass.Name == "AcDbPoint")
                    {//Точка
                        DBPoint Pnt = ent as DBPoint;
                        if (Pnt.Position.Z != 0)
                        {
                            Pnt.Position = new Teigha.Geometry.Point3d(Pnt.Position.X, Pnt.Position.Y, 0);

                            //ed.WriteMessage("DEBUG: Преобразован объект: Точка");
                            result = true;
                            pointcount++;
                        }
                    }
                    else if (id_platf.ObjectClass.Name == "AcDbSpline" )
                    {//Сплайн
                        Spline spl = ent as Spline;

                        //Если сплайн можно расплющить (за это отвечает отдельная функция)
                        if (flattenSpline(ent))
                        {
                            //ed.WriteMessage("DEBUG: Преобразован объект: Сплайн");
                            result = true;
                            splinecount++;
                        }
                    }
                    else if (id_platf.ObjectClass.Name == "AcDbText")
                    { //Текст
                        DBText dbtxt = (DBText)ent;
                        if (dbtxt.Position.Z != 0 || dbtxt.Thickness != 0)
                        {
                            dbtxt.Thickness = 0;
                            dbtxt.Position = new Teigha.Geometry.Point3d(dbtxt.Position.X, dbtxt.Position.Y, 0);
                            //ed.WriteMessage("DEBUG: Преобразован объект: Текст");
                            result = true;
                            txtcount++;
                        }
                    }
                    else if (id_platf.ObjectClass.Name == "AcDbMText")
                    {//Мтекст
                        MText mtxt = (MText)ent;
                        if (mtxt.Location.Z != 0 )
                        {
                            mtxt.Location = new Teigha.Geometry.Point3d(mtxt.Location.X, mtxt.Location.Y, 0);
                            //ed.WriteMessage("DEBUG: Преобразован объект: Мтекст");
                            mtxtcount++;
                            result = true;
                        };
                    }
                    else if (id_platf.ObjectClass.Name == "AcDbHatch")
                    {//Штриховка
                        Teigha.DatabaseServices.Hatch htch = ent as Teigha.DatabaseServices.Hatch;

                        //Если штриховка имеет Elevation не 0 или ее можно расплющить
                        if (htch.Elevation != 0 || FlattenHatch(id_platf))
                        {
                            htch.Elevation = 0;


                            //ed.WriteMessage("DEBUG: Преобразован объект: Штриховка");
                            hatchcount++;
                            result = true;
                        }
                    }
                    else if (id_platf.ObjectClass.Name == "AcDbRotatedDimension")
                    {//Размер повернутый
                        RotatedDimension dim = (RotatedDimension)ent;

                        //Проверяем, имеют ли задающие точки размера ненулевую координату Z
                        if (dim.XLine1Point.Z != 0 || dim.XLine2Point.Z != 0 || dim.DimLinePoint.Z != 0 || dim.TextPosition.Z != 0)
                        {
                            dim.XLine1Point = new Point3d(dim.XLine1Point.X, dim.XLine1Point.Y, 0);
                            dim.XLine2Point = new Point3d(dim.XLine2Point.X, dim.XLine2Point.Y, 0);
                            dim.DimLinePoint = new Point3d(dim.DimLinePoint.X, dim.DimLinePoint.Y, 0);
                            dim.TextPosition = new Point3d(dim.TextPosition.X, dim.TextPosition.Y, 0);

                            //ed.WriteMessage("DEBUG: Преобразован объект: повернутый размер");

                            result = true;
                            dimcount++;
                        };
                    }
                    else if (id_platf.ObjectClass.Name == "AcDbPoint3AngularDimension")
                    {//Угловой размер по 3 точкам
                        Point3AngularDimension dim = (Point3AngularDimension)ent;
                        if (dim.XLine1Point.Z != 0 || dim.XLine2Point.Z != 0 || dim.CenterPoint.Z != 0 || dim.TextPosition.Z != 0)
                        {

                            dim.XLine1Point = new Point3d(dim.XLine1Point.X, dim.XLine1Point.Y, 0);
                            dim.XLine2Point = new Point3d(dim.XLine2Point.X, dim.XLine2Point.Y, 0);
                            dim.CenterPoint = new Point3d(dim.CenterPoint.X, dim.CenterPoint.Y, 0);

                            dim.TextPosition = new Point3d(dim.TextPosition.X, dim.TextPosition.Y, 0);

                            //ed.WriteMessage("DEBUG: Преобразован объект: Угловой размер по трем точкам");

                            result = true;
                            dimcount++;
                        };
                    }
                    else if (id_platf.ObjectClass.Name == "AcDbLineAngularDimension2")
                    {//Еще угловой размер по точкам
                        LineAngularDimension2 dim = (LineAngularDimension2)ent;

                        if (dim.XLine1Start.Z != 0 || dim.XLine1End.Z != 0 || dim.XLine1Start.Z != 0 || dim.XLine2End.Z != 0 || dim.ArcPoint.Z != 0 || dim.TextPosition.Z != 0)
                        {

                            dim.XLine1Start = new Point3d(dim.XLine1Start.X, dim.XLine1Start.Y, 0);
                            dim.XLine1End = new Point3d(dim.XLine1End.X, dim.XLine1End.Y, 0);
                            dim.XLine2Start = new Point3d(dim.XLine2Start.X, dim.XLine2Start.Y, 0);
                            dim.XLine2End = new Point3d(dim.XLine2End.X, dim.XLine2End.Y, 0);
                            dim.ArcPoint = new Point3d(dim.ArcPoint.X, dim.ArcPoint.Y, 0);

                            dim.TextPosition = new Point3d(dim.TextPosition.X, dim.TextPosition.Y, 0);

                            //ed.WriteMessage("DEBUG: Преобразован объект: Угловой размер по 5 точкам");

                            result = true;
                            dimcount++;
                        };
                    }
                    else if (id_platf.ObjectClass.Name == "AcDbDiametricDimension")
                    {  //Размер диаметра окружности
                        DiametricDimension dim = (DiametricDimension)ent;

                        if (dim.FarChordPoint.Z != 0 || dim.ChordPoint.Z != 0 || dim.TextPosition.Z != 0)
                        {
                            dim.FarChordPoint = new Point3d(dim.FarChordPoint.X, dim.FarChordPoint.Y, 0);
                            dim.ChordPoint = new Point3d(dim.ChordPoint.X, dim.ChordPoint.Y, 0);
                            dim.TextPosition = new Point3d(dim.TextPosition.X, dim.TextPosition.Y, 0);

                            //ed.WriteMessage("DEBUG: Преобразован объект: Диаметральный размер");

                            result = true;
                            dimcount++;
                        };
                    }
                    else if (id_platf.ObjectClass.Name == "AcDbArcDimension")
                    {  //Дуговой размер
                        ArcDimension dim = (ArcDimension)ent;

                        if (dim.XLine1Point.Z != 0 || dim.XLine2Point.Z != 0 || dim.ArcPoint.Z != 0 || dim.TextPosition.Z != 0)
                        {
                            dim.XLine1Point = new Point3d(dim.XLine1Point.X, dim.XLine1Point.Y, 0);
                            dim.XLine2Point = new Point3d(dim.XLine2Point.X, dim.XLine2Point.Y, 0);
                            dim.ArcPoint = new Point3d(dim.ArcPoint.X, dim.ArcPoint.Y, 0);
                            dim.TextPosition = new Point3d(dim.TextPosition.X, dim.TextPosition.Y, 0);

                            //ed.WriteMessage("DEBUG: Преобразован объект: Дуговой размер");

                            result = true;
                            dimcount++;
                        };

                    }
                    else if (id_platf.ObjectClass.Name == "AcDbRadialDimension")
                    {  //Радиальный размер
                        RadialDimension dim = (RadialDimension)ent;

                        if (dim.Center.Z != 0 || dim.ChordPoint.Z != 0 || dim.TextPosition.Z != 0)
                        {
                            dim.Center = new Point3d(dim.Center.X, dim.Center.Y, 0);
                            dim.ChordPoint = new Point3d(dim.ChordPoint.X, dim.ChordPoint.Y, 0);
                            dim.TextPosition = new Point3d(dim.TextPosition.X, dim.TextPosition.Y, 0);

                            //ed.WriteMessage("DEBUG: Преобразован объект: Радиальный размер");

                            result = true;
                            dimcount++;
                        };

                    }
                    else if (id_platf.ObjectClass.Name == "AcDbEllipse")
                    {  //Эллипс
                        Ellipse el = (Ellipse)ent;

                        /*if (el.Center.Z != 0 || el.StartPoint.Z != 0 || el.EndPoint.Z != 0)*/

                        if (el.Center.Z != 0)
                        {

                            el.Center = new Point3d(el.Center.X, el.Center.Y, 0);
                            /*
                            BUG: не поддерживается платформой перемещение начальной и конечной точек эллипса
                            el.StartPoint = new Point3d(el.StartPoint.X, el.StartPoint.Y, 0);
                            el.EndPoint = new Point3d(el.EndPoint.X, el.EndPoint.Y, 0);
                            */

                            //ed.WriteMessage("DEBUG: Преобразован объект: Эллипс");

                            result = true;
                            ellipsecount++;
                        };

                    }
                    else if (id_platf.ObjectClass.Name == "AcDbAttributeDefinition")
                    {  //Атрибут блока
                        AttributeDefinition ad = (AttributeDefinition)ent;

                        if (ad.Position.Z != 0)
                        {
                            //ed.WriteMessage("DEBUG: Преобразован объект: Атрибут блока");
                            ad.Position = new Point3d(ad.Position.X, ad.Position.Y, 0);

                            result = true;
                            attributedefcount++;
                        };
                    }

                    else if (id_platf.ObjectClass.Name == "AcDbLeader")
                    {  //Выноска Autocad
                        Leader ld = (Leader)ent;

                        if (ld.EndPoint.Z != 0 || ld.StartPoint.Z != 0)
                        {
                            //ed.WriteMessage("DEBUG: Преобразован объект: Выноска Autocad");

                            ld.EndPoint = new Point3d(ld.EndPoint.X, ld.EndPoint.Y, 0);
                            ld.StartPoint = new Point3d(ld.StartPoint.X, ld.StartPoint.Y, 0);

                            result = true;
                            leadercount++;
                        };

                    }
                    /*
                else if (id_platf.ObjectClass.Name == "AcDbPolygonMesh")
                {
                     BUG: В платформе нет API для доступа к вершинам сетей AcDbPolygonMesh и AcDbPolygonMesh и AcDbSurface
                     
                }
                else if (id_platf.ObjectClass.Name == "AcDbSolid")
                {
                     BUG: Чтобы плющить Solid-ы нужны API функции 3d
                }
                else if (id_platf.ObjectClass.Name == "AcDbRegion")
                {
                    Region rgn = ent as Region;
                    BUG: нет свойств у региона
                }
                
                */
                    else
                    {
                        //Если объект не входит в число перечисленных типов,
                        //то выводим в командную строку класс этого необработанного объекта

                        ed.WriteMessage("Не могу обработать тип объекта: " + id_platf.ObjectClass.Name);
                        noflatcount++;
                    }

                    //В конце концов осуществляем транзакцию
                    transaction.Commit();
                }
                catch (PlatformDb.Runtime.Exception ex)
                {
                    //Если что-то сломалось, то в командную строку выводится ошибка
                    ed.WriteMessage("Не могу преобразовать - ошибка: " + ex.Message);
                };

                //Возвращаем значение функции
                return result;
            }

            private bool flattenSpline(Entity ent)
            {
                /*
                Функция плющит сплайн, последовательно зануляя координату Z каждой из его вершин
                На входе принимает Entity
                выдает true, если какая-либо из контрольных точек имела ненулевую кординату Z
                */
                bool result = false;

                //Исходный пример из AutoCAD:
                //http://through-the-interface.typepad.com/through_the_interface/2007/04/iterating_throu.html
                //сильно в нем не разбирался, просто адаптирован.

                Transaction tr = acCurDb.TransactionManager.StartTransaction();

                Spline spl = ent as Spline;
                if (spl != null)
                {
                    // Количество контрольных точек сплайна
                    int vn = spl.NumControlPoints;

                    //Цикл по всем контрольным точкам сплайна
                    for (int i = 0; i < vn; i++)
                    {
                        // Could also get the 3D point here
                        Point3d pt = spl.GetControlPointAt(i);
                        if (pt.Z != 0)
                        {
                            spl.SetControlPointAt(i, new Point3d(pt.X, pt.Y, 0));
                            result = true;
                        }
                    }
                }
                tr.Commit();

                return result;
            }

            public bool flattenPolyline(Entity entline)
            {
                /*
                 * Функция принимает на входе Entity и возвращает true, если эта
                 * Entity является простой полилинией или 2dPolyline
                 * И хотя бы одна из вершин этой полилинии имеет ненулевую координату Z 
                */
                bool result = false;

                //Исходный пример из AutoCAD .Net
                //http://through-the-interface.typepad.com/through_the_interface/2007/04/iterating_throu.html


                Transaction tr = acCurDb.TransactionManager.StartTransaction();
                // If a "lightweight" (or optimized) polyline
                Polyline lwp = entline as Polyline;

                if (lwp != null)
                {
                    // Use a for loop to get each vertex, one by one
                    int vn = lwp.NumberOfVertices;
                    for (int i = 0; i < vn; i++)
                    {
                        // Could also get the 3D point here
                        Point3d pt = lwp.GetPoint3dAt(i);
                        if (pt.Z != 0)
                        {
                            //Назначаем новую вершину полилинии
                            lwp.SetPointAt(i, new Point2d(pt.X, pt.Y));
                            result = true;
                        }
                    }
                }
                else
                {
                    // If an old-style, 2D polyline
                    Polyline2d p2d = entline as Polyline2d;
                    if (p2d != null)
                    {
                        // Use foreach to get each contained vertex
                        foreach (ObjectId vId in p2d)
                        {
                            Vertex2d v2d =
                              (Vertex2d)tr.GetObject(
                                vId,
                                OpenMode.ForWrite
                              );
                            if (v2d.Position.Z != 0)
                            {
                                v2d.Position = new Point3d(v2d.Position.X, v2d.Position.Y, 0);
                                result = true;
                            };
                        }
                    }
                    else
                    {
                        // If an old-style, 3D polyline
                        Polyline3d p3d = entline as Polyline3d;
                        if (p3d != null)
                        {
                            // Use foreach to get each contained vertex
                            foreach (ObjectId vId in p3d)
                            {
                                PolylineVertex3d v3d =
                                  (PolylineVertex3d)tr.GetObject(
                                    vId,
                                    OpenMode.ForWrite
                                  );
                                if (v3d.Position.Z != 0)
                                {
                                    v3d.Position = new Point3d(v3d.Position.X, v3d.Position.Y, 0);
                                    result = true;
                                };
                            }
                        }
                    }
                }
                // Committing is cheaper than aborting
                tr.Commit();

                return result;
            }

            private bool ExplodeBlock(ObjectId BlockId, bool eraseOrig)
            {
                /*
                 * Функция взрывает блок и плющит то что осталось от взрыва
                 * Принимает на входе ObjectId блока
                 * И eraseOrig переменную, которая управляет удалением исходного блока
                 * 
                 */


                //Считаем что пока в блоке нечего плющить:
                bool SomethingToFlatten = false;

                //Открываем транзакцию
                Transaction tr =
                  acCurDb.TransactionManager.StartTransaction();
                using (tr)
                {
                    // Это коллекция объектов, которая будет включать все элементы взорванного блока
                    DBObjectCollection objs = new DBObjectCollection();

                    //Открываем на чтение блок
                    Entity ent =
                      (Entity)tr.GetObject(
                        BlockId,
                        OpenMode.ForRead
                      );

                    // Взрываем блок в нашу коллекцию объектов
                    ent.Explode(objs);

                    // Открываем текущее пространство на запись 
                    BlockTableRecord btr =
                      (BlockTableRecord)tr.GetObject(
                        acCurDb.CurrentSpaceId,
                        OpenMode.ForWrite
                      );

                    // Пробегаем по коллекции объектов и 
                    // каждый из них добавляем к текущему пространству
                    foreach (DBObject obj in objs)
                    {
                        //преобразуем объект к Entity
                        Entity entExplode = (Entity)obj;
                        //Добавляем эту Entity в пространство
                        btr.AppendEntity(entExplode);
                        //Добавляем к транзакции новые объекты
                        tr.AddNewlyCreatedDBObject(entExplode, true);

                        //Проверяем, есть ли в составе блока объекты, 
                        //которые нужно расплющить (и в этом случае все входящие блоки плющим)
                        //Покольку только исходный блок нужно оставить, а все рекурсивно входящие в него
                        //подблоки в этом случае можно удалить.
                        if (FlattenByPlatform(entExplode.ObjectId, true))
                        {
                            //Здесь получается рекурсивный вызов плющилки с принудительным взрывом блоков
                            SomethingToFlatten = true;
                        }
                    };
                    
                    //Если блок плоский, то и нечего его взрывать
                    if (!SomethingToFlatten)
                    {
                        //Удаляем что мы навзрывали - объекты не нужны на чертеже
                        foreach (DBObject obj in objs)
                        {
                            Entity entExplode = obj as Entity;
                            entExplode.Erase();
                        }
                        //Соответственно, если были неплоские примитивы, то результаты взрыва и расплющивания блока
                        //остаются на чертеже
                    };
                    
                    //Проверим, если нужно - удалим исходный блок
                    if (eraseOrig)
                    {
                        ent.UpgradeOpen();//открываем блок на запись
                        //и удаляем
                        ent.Erase();
                    };

                    // And then we commit
                    tr.Commit();

                    //Возвращаем значение (было что плющить)
                    return SomethingToFlatten;
                }
            }

            private bool ExplodeProxy(ObjectId ProxyId, bool eraseOrig)
            {
                /*
                 * Функция взрывает блок и плющит то что осталось от взрыва
                 * Принимает на входе ObjectId блока
                 * И eraseOrig переменную, которая управляет удалением исходного блока
                 * 
                 */


                //Считаем что пока в блоке нечего плющить:
                bool SomethingToFlatten = false;

                //Открываем транзакцию
                Transaction tr =
                  acCurDb.TransactionManager.StartTransaction();
                using (tr)
                {
                    // Это коллекция объектов, которая будет включать все элементы взорванного блока
                    DBObjectCollection objs = new DBObjectCollection();

                    //Открываем на чтение блок
                    Entity ent =
                      (Entity)tr.GetObject(
                        ProxyId,
                        OpenMode.ForRead
                      );

                    // Взрываем блок в нашу коллекцию объектов
                    ent.Explode(objs);

                    // Открываем текущее пространство на запись 
                    BlockTableRecord btr =
                      (BlockTableRecord)tr.GetObject(
                        acCurDb.CurrentSpaceId,
                        OpenMode.ForWrite
                      );

                    // Пробегаем по коллекции объектов и 
                    // каждый из них добавляем к текущему пространству
                    foreach (DBObject obj in objs)
                    {
                        //преобразуем объект к Entity
                        Entity entExplode = (Entity)obj;
                        //Добавляем эту Entity в пространство
                        btr.AppendEntity(entExplode);
                        //Добавляем к транзакции новые объекты
                        tr.AddNewlyCreatedDBObject(entExplode, true);

                        //Проверяем, есть ли в составе блока объекты, 
                        //которые нужно расплющить (и в этом случае все входящие блоки плющим)
                        //Поскольку только исходный блок нужно оставить, а все рекурсивно входящие в него
                        //подблоки в этом случае можно удалить.
                        if (FlattenByPlatform(entExplode.ObjectId, true))
                        {
                            //Здесь получается рекурсивный вызов плющилки с принудительным взрывом блоков
                            SomethingToFlatten = true;
                        }
                    };
                    //Если блок плоский, то и нечего его взрывать
                    if (!SomethingToFlatten)
                    {
                        //Удаляем что мы навзрывали - объекты не нужны на чертеже
                        foreach (DBObject obj in objs)
                        {
                            Entity entExplode = obj as Entity;
                            entExplode.Erase();
                        }
                        //Соответственно, если были неплоские примитивы, то результаты взрыва и расплющивания блока
                        //остаются на чертеже
                    };

                    //Проверим, если нужно - удалим исходный блок
                    if (eraseOrig)
                    {
                        ent.UpgradeOpen();//открываем блок на запись
                        //и удаляем
                        ent.Erase();
                    };

                    // And then we commit
                    tr.Commit();

                    //Возвращаем значение (было что плющить)
                    return SomethingToFlatten;
                }
            }

            private bool FlattenHatch(ObjectId hatchId)
            {
                /*
                 * Функция преобразования координат контура штриховки
                 * Последовательно пробегает по каждому из контуров штриховки
                 * далее последовательно пробегает по каждой из вершин данного контура
                 * и зануляет координату Z этой вершины
                */

                //Исходный код для AutoCAD .Net
                //http://forums.autodesk.com/t5/NET/Restore-hatch-boundaries-if-they-have-been-lost-with-NET/m-p/3779514#M33429

                bool result = false;


                using (Transaction tr = acCurDoc.TransactionManager.StartTransaction())
                {
                    Hatch hatch = tr.GetObject(hatchId, OpenMode.ForRead) as Hatch;
                    if (hatch != null)
                    {
                        BlockTableRecord btr = tr.GetObject(hatch.OwnerId, OpenMode.ForWrite) as BlockTableRecord;
                        if (btr != null)
                        {
                            Plane plane = hatch.GetPlane();
                            int nLoops = hatch.NumberOfLoops;
                            for (int i = 0; i < nLoops; i++)
                            {//Цикл по каждому из контуров штриховки
                                //Проверяем что контур является полилинией
                                HatchLoop loop = hatch.GetLoopAt(i);
                                if (loop.IsPolyline)
                                {
                                    using (Polyline poly = new Polyline())
                                    {
                                        //Создаем полилинию из точек контура
                                        int iVertex = 0;
                                        foreach (BulgeVertex bv in loop.Polyline)
                                        {
                                            poly.AddVertexAt(iVertex++, bv.Vertex, bv.Bulge, 0.0, 0.0);
                                        }
                                        //Создаем полилиню в текущем пространстве
                                        ObjectId polyId = btr.AppendEntity(poly);
                                        tr.AddNewlyCreatedDBObject(poly, true);

                                        //Плющим полученный контур штриховки
                                        if (flattenPolyline(poly as Entity))
                                        {
                                            //Создание штриховки: http://adndevblog.typepad.com/autocad/2012/07/hatch-using-the-autocad-net-api.html#sthash.ed0Ms37Y.dpuf
                                            ObjectIdCollection ObjIds = new ObjectIdCollection();
                                            ObjIds.Add(polyId);

                                            //Задаем на всякий случай штриховке Elevation =0;
                                            hatch.Elevation = 0;
                                            //Удаляем старый контур
                                            hatch.RemoveLoopAt(i);

                                            //Добавляем новый контур штриховки из сплющенной полилинии
                                            hatch.AppendLoop((int)HatchLoopTypes.Default, ObjIds);
                                            hatch.EvaluateHatch(true);

                                            result = true;
                                        }
                                        else
                                        {
                                            //Ну если полилиния не нуждается в расплющивании, то ее можно удалить.
                                            poly.Erase();
                                        }
                                    }
                                }
                                else
                                {//Если не удалось преобразовать контур к полилинии

                                    //Выводим сообщение в командную строку
                                    ed.WriteMessage("Ошибка обработки: Контур штриховки - не полилиния");
                                    noflatcount++;
                                    //Не будем брать исходный код для штриховок, контур который не сводится к полилинии
                                }
                            }
                        }
                    }
                    tr.Commit();
                }
                return result;
            }
        }
        class linetypeExtractor
        {
            //Общие объекты
            Database acCurDb = Platform.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database;
            Document acDoc = Platform.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = Platform.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;

            [CommandMethod("OGPextractLineType")]
            public void ExtractLineType()
            {
                //Функция пробегается по всем типам линий, имеющимся в документе
                //и формирует их описание.
                //Запрашивает имя файла и сохраняет *.lin файл с описаниями.

                //Хоть какой-то пример c типами линий
                //http://docs.autodesk.com/ACD/2013/ENU/index.html?url=files/GUID-81423588-A182-4511-B9D3-115014C96BCE.htm,topicNumber=d30e727803

                // Start a transaction
                using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
                {
                    //Открываем OpenDialog (для этого к проекту нужно подключить Reference System.Windows.Forms)
                    SaveFileDialog save = new SaveFileDialog();
                    //Задаем стартовую директорию и дефолтное имя файла (такое же, как у исходного документа)
                    save.InitialDirectory = Path.GetDirectoryName(acCurDb.Filename);
                    save.FileName = Path.GetFileNameWithoutExtension(acCurDb.Filename) + ".lin";
                    save.Filter = "Файл типов линий | *.lin";

                    //Если все впорядке с именем файла, то приступим к обработке
                    if (save.ShowDialog() == DialogResult.OK)
                    {

                        //Счетчики обработанных и необработанных типов линий
                        int exportedLtCount = 0;
                        int noExportedLtCount = 0;

                        //Открываем на запись файл, указанный в OpenDialog
                        //Здесь важно, что бы кодировка была ANSI, т.к. UTF будет читаться некорректно
                        StreamWriter writer = new StreamWriter(save.OpenFile(), Encoding.GetEncoding(1251));

                        //Открываем на чтение таблицу типов линий
                        LinetypeTable acLineTypTbl;
                        acLineTypTbl = acTrans.GetObject(acCurDb.LinetypeTableId,
                                                         OpenMode.ForRead) as LinetypeTable;


                        foreach (ObjectId ObjLt in acLineTypTbl)
                        {
                            //В таблице типов линий по каждой записи создаем объект
                            DBObject dbo = acTrans.GetObject(ObjLt, OpenMode.ForRead);
                            LinetypeTableRecord ltRec = dbo as LinetypeTableRecord;


                            if (!(ltRec.Name == "ByLayer" || ltRec.Name == "ByBlock" || ltRec.Name == "Continuous")) //Пропускаем системные типы линий
                            {
                                //В справке AutoCAD прекрасно описана структура .lin файла
                                //Описание состоит из двух строк. Первая строка: "*GOST2.303 3,Сплошная волнистая ~~~~~"
                                string strLnDesc = "*" + ltRec.Name + "," + ltRec.Comments + Environment.NewLine;
                                //Вторая строка начинается с А, и далее последовательно по типам штрихов: "A,0.001,[WAVE,GOST 2.303-68.shx,S=1,X=0,Y=0],-26"

                                //Начало
                                string strLnDef = "A,";

                                //Цикл по всем штрихам в типе линий
                                for (int ii = 0; ii < ltRec.NumDashes; ii++)
                                {
                                    //Завернуто тут - все-таки к объектам обращаемся, может что-нибудь и упасть
                                    try
                                    {

                                        strLnDef += ltRec.DashLengthAt(ii).ToString();   //Длина штриха
                                        if (ii != ltRec.NumDashes - 1) strLnDef += ",";  //После длины ставится запятая
                                        else strLnDef += Environment.NewLine;            //Если штрих последний - то запятая не ставится - нужен перевод строки


                                        if (!ltRec.ShapeStyleAt(ii).IsNull)             //Если штрих - не обычная линия (текст или Shape)
                                        {
                                            //Находим объект - текстовый стиль
                                            TextStyleTableRecord actxtrec = acTrans.GetObject(ltRec.ShapeStyleAt(ii), OpenMode.ForRead) as TextStyleTableRecord;


                                            if (ltRec.TextAt(ii) != "") //Если штрих = текст
                                            {
                                                strLnDef += "[\"";
                                                strLnDef += ltRec.TextAt(ii) + "\"," + actxtrec.Name;           //Сам текст в ковычках + имя текстового стиля
                                                strLnDef += ",S=" + ltRec.ShapeScaleAt(ii);                     //Масштаб
                                                strLnDef += ",R=" + 180 * ltRec.ShapeRotationAt(ii) / Math.PI;  //Поворот
                                                strLnDef += ",X=" + ltRec.ShapeOffsetAt(ii).X;                  //Смещение Х
                                                strLnDef += ",Y=" + ltRec.ShapeOffsetAt(ii).Y;                  //Смещение У
                                                strLnDef += "],";
                                            }
                                            else
                                            {
                                                strLnDef += "[";
                                                strLnDef += GetShapeName(ltRec.ShapeNumberAt(ii), ltRec.ShapeStyleAt(ii)) + ",";    //Создаем форму и получаем ее имя
                                                strLnDef += actxtrec.FileName;                                                     //Выводим имя shx формы
                                                strLnDef += ",S=" + ltRec.ShapeScaleAt(ii);                                           //Масштаб
                                                strLnDef += ",R=" + 180 * ltRec.ShapeRotationAt(ii) / Math.PI;                      //Угол поворота
                                                strLnDef += ",X=" + ltRec.ShapeOffsetAt(ii).X;                                      //Смещение Х
                                                strLnDef += ",Y=" + ltRec.ShapeOffsetAt(ii).Y;                                      //Смещение У
                                                strLnDef += "],";
                                            }
                                        }
                                    }
                                    catch (PlatformDb.Runtime.Exception ex)
                                    {
                                        //Если что-то сломалось, то в командную строку выводится ошибка
                                        ed.WriteMessage("Ошибка извлечения описания типа: " + ex.Message);
                                        noExportedLtCount++;
                                    };
                                };

                                if (strLnDef != "A,") //Проверяем, что описание было не пустым и что-то получилось
                                {
                                    //Пишем строчку-описание в файл
                                    writer.WriteLine(strLnDesc + strLnDef);
                                    //Увиличиваем счетчик обработанных типов линий
                                    exportedLtCount++;
                                }
                            }
                        }

                        // Завершаем транзакцию
                        acTrans.Commit();
                        // Закрываем открытый на запись файл
                        writer.Dispose();
                        writer.Close();

                        //Выводим в командную строку результаты экспорта
                        if (exportedLtCount != 0) ed.WriteMessage(string.Format("Экспортировано {0} типов линий", exportedLtCount));
                        if (noExportedLtCount != 0) ed.WriteMessage(string.Format("Не удалось экспортировать {0} типов линий", noExportedLtCount));
                    };
                }
            }

            private string GetShapeName(int ShpNumber, ObjectId ShapeStyleId)
            {
                //Функция возвращает имя Shape по заданному индексу и стилю текста
                string result = "";

                Transaction tr = Platform.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction();
                try
                {
                    //Далее - обычная шелуха для создания объекта
                    //http://docs.autodesk.com/ACD/2010/ENU/AutoCAD%20.NET%20Developer's%20Guide/index.html?url=WS1a9193826455f5ff2566ffd511ff6f8c7ca-3b2a.htm,topicNumber=d0e36064
                    // Open the Block table for read
                    BlockTable acBlkTbl;
                    acBlkTbl = tr.GetObject(Platform.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database.BlockTableId,
                                                 OpenMode.ForRead) as BlockTable;

                    // Open the Block table record Model space for write
                    BlockTableRecord acBlkTblRec;
                    acBlkTblRec = tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                    OpenMode.ForWrite) as BlockTableRecord;

                    Shape shp = new Shape();                                //Создаем форму

                    shp.ShapeNumber = (short)ShpNumber;                     //Назначаем ей нужный номер
                    shp.StyleId = ShapeStyleId;                             //Назначаем стиль
                    shp.Position = new Teigha.Geometry.Point3d(0, 0, 0);    //Кладем ее в начало координат

                    acBlkTblRec.AppendEntity(shp);                          //Добавляем к BlockTableRecord

                    tr.AddNewlyCreatedDBObject(shp, true);                  //Коммитим
                    tr.Commit();

                    result = shp.Name;                                      //Забираем из созданной формы имя
                    shp.Erase();                                            //И удаляем ее
                }
                catch (PlatformDb.Runtime.Exception ex)
                {
                    //Если что-то сломалось, выдадим исключение
                    ed.WriteMessage("Ошибка экспорта: "+ex.Message);
                    tr.Abort();
                };

                return result;
            }
        }
    }

}