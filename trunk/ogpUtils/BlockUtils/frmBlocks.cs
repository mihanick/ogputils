using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
//Классы Teigha
using Teigha.Runtime; // Не работает вместе c Multicad.Runtime
using Platform = HostMgd;
using PlatformDb = Teigha;
using Teigha.DatabaseServices;

//using Multicad.Runtime;


namespace ogpUtils
{

    public partial class frmBlks : Form
    {
        //Основной диалог - таблица блоков с флажками атрибутов   
 
        [CommandMethod("OGPblockRename")]
        public void CommandBlockRename()
        {
            //Команда вызова формы блоков
            ogpUtils.frmBlks FormOfBlocks = new ogpUtils.frmBlks();
            HostMgd.ApplicationServices.Application.ShowModelessDialog(FormOfBlocks);
        }
        
        public frmBlks()
        {
            InitializeComponent();
            
            //При запуске команды создаем класс коллекции блоков и наполняем список
            BlockUtils.DrawingBlocksCollection bc = new BlockUtils.DrawingBlocksCollection();
            FillList(bc.ListOfBlocks);
        }
       
        public void FillList(List<BlockUtils.BlockProps> ManyBlocks)
        {
            //Функция заполняет список блоков на основании списка блоков, полученного из чертежа

            //Таблица данных
            System.Data.DataTable dt = new System.Data.DataTable();

            //Добавляем колонки в таблицу 
            //(здесь и далее класс BlockFieldsNames. используется для получения текстовых строчек - имен столбцов - я сделалчтобы не ошибиться)
            dt.Columns.Add(BlockFieldNames.blockName, typeof(string));
            dt.Columns.Add(BlockFieldNames.Explodable, typeof(bool));
            dt.Columns.Add(BlockFieldNames.UniformScale, typeof(bool));
            dt.Columns.Add(BlockFieldNames.blockId,typeof(ObjectId));

            //Пробегаем по списку блоков
            foreach (BlockUtils.BlockProps OneBlock in ManyBlocks)
            {
                //Преобразуем BlockScaling.Uniform в true - для отображения в виде чекбокса
                bool unfScl = (OneBlock.UniformScale == BlockScaling.Uniform);
                //Добавляем строчку - блока с его параметрами Имя, Взрываемый, Равный масштаб, Id блока
                dt.Rows.Add(OneBlock.BlockName, OneBlock.Explodable, unfScl, OneBlock.BlockId);

                //Колонка id блока является скрытой и нужна для того, чтобы потом можно было найти
                //Какой именно блок нужно изменить в чертеже при редактировании его в таблице
            }

            
            //Отклчаем автогенерацию столбцов (они у нас уже определены в визуальном редакторе формы)
            dgv1.AutoGenerateColumns = false;
            //Установка связи контрола dgv1 с полями данных в таблице dt
            dgv1.Columns[BlockFieldNames.blockName].DataPropertyName = BlockFieldNames.blockName;
            dgv1.Columns[BlockFieldNames.Explodable].DataPropertyName = BlockFieldNames.Explodable;
            dgv1.Columns[BlockFieldNames.UniformScale].DataPropertyName = BlockFieldNames.UniformScale;
            dgv1.Columns[BlockFieldNames.blockId].DataPropertyName = BlockFieldNames.blockId;

            //Подключаем источник данных (здесь важно сначала установить связь, а только потом подключить источник данных).
            dgv1.DataSource = dt;         
        }

        private void dgv1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            //Функция обрабатывает редактирование в ячейке таблицы
            try
            {
                //Создаем класс обработки блоков
                BlockUtils.DrawingBlocksCollection blkcollection = new BlockUtils.DrawingBlocksCollection();

                //Получаем таблицу данных из контрола dgv1
                System.Data.DataTable dt = dgv1.DataSource as System.Data.DataTable;

                if (dt != null)
                {
                    //Получаем ID редактируемого блока из контрола, преобразуем к типу ObjectId
                    ObjectId idEdited = (ObjectId)dgv1.Rows[e.RowIndex].Cells[BlockFieldNames.blockId].Value;

                    //Пробегаем по всем строчкам в таблице чтобы найти нужную
                    foreach (DataRow row in dt.Rows)
                        if (row != null && (ObjectId)row[BlockFieldNames.blockId]==idEdited)
                        {
                            //Создаем переменную, которая воспримет атрибуты блока из ячеек dgv1
                            BlockUtils.BlockProps bp = new BlockUtils.BlockProps();

                            //Назначаем соответствие атрибутов столбцам
                            bp.BlockId = (ObjectId)row[BlockFieldNames.blockId];
                            bp.BlockName = (string)row[BlockFieldNames.blockName];
                            bp.Explodable = (bool)row[BlockFieldNames.Explodable];
                            if ((bool)row[BlockFieldNames.UniformScale]) bp.UniformScale = BlockScaling.Uniform;

                            //Запускаем применение новых свойств в чертеже
                            blkcollection.SetBlockProperties(bp);
                        }
                }
            }
            catch (PlatformDb.Runtime.Exception ex)
            {
                //Если что-то сломалось, то в командную строку выводится ошибка
                Platform.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor
                    .WriteMessage("Не могу отправить изменения - ошибка: " + ex.Message);
            };
        }
    }

    public sealed class BlockFieldNames
    {
        //Этот класс нужен просто как строковый enum - чтобы не ошибиться в названиях столбцов

        //http://www.codeproject.com/Articles/11130/String-Enumerations-in-C
        private BlockFieldNames() { }

        public static readonly string blockName = "blockName";
        public static readonly string Explodable = "Explodable";
        public static readonly string UniformScale = "UniformScale";
        public static readonly string blockId = "blockId";
    }  
}
