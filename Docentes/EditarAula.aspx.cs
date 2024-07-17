using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using BusinessData.Entities;
using BusinessData.BusinessLogic;
using System.Collections.Generic;
using BusinessData.DataAccess;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Linq;

public partial class Docentes_EditarAula : System.Web.UI.Page
{          
    AulaBO aulaBo = new AulaBO();
    TurmaBO turmaBo = new TurmaBO();
    CategoriaDataBO cdataBo = new CategoriaDataBO();
    RequisicoesBO reqBo = new RequisicoesBO();
    CategoriaAtividadeBO categoriaBo = new CategoriaAtividadeBO();
    CategoriaRecursoBO categoriaRecursoBo = new CategoriaRecursoBO();
    List<Guid> categorias = new List<Guid>();
    List<Color> argb = new List<Color>();
    List<CategoriaData> listCData = new List<CategoriaData>();
    List<CategoriaAtividade> listaAtividades = new List<CategoriaAtividade>();
    Calendario cal;
    private int cont = 1;
    Guid dummyGuid = new Guid();
	bool facin = true;
    bool semRecursos = true;
    int totalAulas = 0;

    protected void Page_Load(object sender, EventArgs e)
    {
        string cs = ConfigurationManager.ConnectionStrings["SARCFACINcs"].ConnectionString;
        if (cs.Contains("SARCDEV"))
        {
            semRecursos = false;            
        }
        try
        {
            if (!IsPostBack)
            {
                if (Session["AppState"] != null && ((AppState)Session["AppState"]) == AppState.Admin)
                {
                    Server.Transfer("~/Default/Erro.aspx?Erro=O sistema est� bloqueado.");
                }
                //else if ((AppState)Session["AppState"] != AppState.Requisicoes)
                //    Server.Transfer("~/Default/Erro.aspx?Erro=Os recursos j� foram distribu�dos.");
                else
                {
                    if (Session["Calendario"] == null)
                    {
                        Response.Redirect("../Default/SelecionarCalendario.aspx");
                    }
                    Guid idturma = new Guid();
                    if (Request.QueryString["GUID"] != null)
                    {
                        try
                        {
                            idturma = new Guid(Request.QueryString["GUID"]);
                        }
                        catch (FormatException)
                        {
                            Response.Redirect("~/Default/Erro.aspx?Erro=Codigo de turma inv�lido!");
                        }
                        Session["TurmaId"] = idturma;
                        cal = (Calendario)Session["Calendario"];

                        CategoriaAtividadeBO cateBO = new CategoriaAtividadeBO();
                        listaAtividades = cateBO.GetCategoriaAtividade();
                        AulaBO AulaBO = new AulaBO();
                        List<Aula> listaAulas = null;
                        try
                        {
                            listaAulas = AulaBO.GetAulas(idturma);
                        }
                        catch (Exception)
                        {
                            Response.Redirect("~/Default/Erro.aspx?Erro=Codigo de turma inv�lido!");
                        }                        

                        foreach (Aula a in listaAulas)
                        {
                            categorias.Add(a.CategoriaAtividade.Id);
                            argb.Add(a.CategoriaAtividade.Cor);
                        }
						
						Disciplina disc = listaAulas[0].TurmaId.Disciplina;
						CategoriaDisciplina cat = disc.Categoria;
						//lblTitulo.text += " " + cat.Descricao;
						
						// Mega gambiarra master extended++
						// TODO: retirar assim que poss�vel!
						if(cat.Descricao.IndexOf("Outras Unidades") != -1)
							facin = false;

                        Disciplina d = listaAulas[0].TurmaId.Disciplina;
                        lblTitulo.Text = listaAulas[0].TurmaId.Disciplina.NomeCodCred + " - Turma " + listaAulas[0].TurmaId.Numero + " - " + Regex.Replace(listaAulas[0].TurmaId.Sala, "32/A", "32");
						Session["facin"] = facin;

                        int horasRelogioEsperadas = d.Cred * 15;
                        int durPeriodo = 45;
                        /*                    if (listaAulas[0].Hora == "JK" || listaAulas[0].Hora == "LM" || listaAulas[0].Hora == "NP"
                                               || listaAulas[1].Hora == "JK" || listaAulas[1].Hora == "LM" || listaAulas[1].Hora == "NP"
                                               || listaAulas[1].Hora == "JK" || listaAulas[2].Hora == "LM" || listaAulas[2].Hora == "NP")
                                                durPeriodo = 45;*/

                        totalAulas = 0;
                        bool emG2 = false;
                        bool haG2 = false;
                        int totalFeriados = 0;
                        foreach (Aula a in listaAulas)
                        {
                            categorias.Add(a.CategoriaAtividade.Id);
                            argb.Add(a.CategoriaAtividade.Cor);
                            if (a.Data >= cal.InicioG2)
                            {
                                //if (a.CategoriaAtividade.Descricao == "Prova de G2")                            
                                //Debug.WriteLine("EM G2");
                                emG2 = true;
                            }
                            if (a.DescricaoAtividade.StartsWith("Feriado") || a.DescricaoAtividade.StartsWith("Suspens�o"))
                                totalFeriados++;
                            if (a.CategoriaAtividade.Descricao == "Prova de G2")
                                haG2 = true;
                            if (!a.DescricaoAtividade.StartsWith("Feriado") && !a.DescricaoAtividade.StartsWith("Suspens�o")
                                && a.CategoriaAtividade.Descricao != "Prova de G2" && !emG2)
                                totalAulas++;
                        }
                        // Contando mais uma aula por causa da G2 que pulamos antes
//                        if (haG2)
//                            totalAulas++;
                        int totalEfetivo = totalAulas * 2 * durPeriodo / 60;
                        int complementares = horasRelogioEsperadas - totalEfetivo;
                        if (complementares < 0) complementares = 0;
                        //lblHoras.Text = "Dura��o do per�odo: " + durPeriodo + " - Horas esperadas: " + horasRelogioEsperadas + " - Horas efetivas: " + totalEfetivo
                        //    + " - <b>Previs�o de horas extraclasse: " + (horasRelogioEsperadas - totalEfetivo) + "</B>";

                        //                    int minutosEsperados = horasRelogioEsperadas * 60;
                        int minutosFeriado = durPeriodo * totalFeriados * 2;
                        int minutosEsperados = durPeriodo * 2 * 18 * d.Cred / 2;
                        minutosFeriado = 0;
                        int horasMinistradas = (minutosEsperados - minutosFeriado) / 60;
                        int extraClasse = horasRelogioEsperadas -horasMinistradas;
                        Debug.WriteLine("Total aulas: " + totalAulas);
                        Debug.WriteLine("Minutos feriado: " + minutosFeriado);
                        Debug.WriteLine("Minutos esperados: " + minutosEsperados);

                        lblHoras.Text = "- Horas esperadas: " + horasRelogioEsperadas + " - Horas efetivas: " + totalEfetivo
                            + " - <b>Previs�o de horas para TDE: " + complementares + "</B>";
						
						dgAulas.DataSource = listaAulas;                        
                        dgAulas.DataBind();
                    }
                }
            }
        }
        catch (DataAccessException ex)
        {
            Response.Redirect("~/Default/Erro.aspx?Erro=" + ex.Message);
        }

    }

    protected Data VerificaData(DateTime dt)
    {
        foreach (Data data in cal.Datas)
            if (dt == data.Date)
                return data;
        return null;
    }

    protected void dgAulas_ItemDataBound(object sender, DataGridItemEventArgs e)
    {
        if (e.Item.ItemType == ListItemType.AlternatingItem || e.Item.ItemType == ListItemType.Item)
        {
            DropDownList ddlAtividade = (DropDownList)e.Item.FindControl("ddlAtividade");          
            Label lblData = (Label)e.Item.FindControl("lblData");
            TextBox txtDescricao = (TextBox)e.Item.FindControl("txtDescricao");
            Label lblDescData = (Label)e.Item.FindControl("lblDescData");
            Label lblCorDaData = (Label)e.Item.FindControl("lblCorDaData");
            Label lblRecursosSelecionados = (Label)e.Item.FindControl("lblRecursosSelecionados");
            Label lblAulaId = (Label)e.Item.FindControl("lblAulaId");

            Panel pnRecursos = (Panel)e.Item.FindControl("pnRecursos");
            HtmlTable tabRecursos = (HtmlTable)e.Item.FindControl("tabRecursos");
            int i = tabRecursos.Rows[0].Cells[0].Controls.Count;
            CheckBoxList cbRecursos = (CheckBoxList)tabRecursos.Rows[0].Cells[0].Controls[1];
            ImageButton butDel = (ImageButton)e.Item.FindControl("butDeletar");

            Color cor = argb[0];

            txtDescricao.Attributes.Add("onkeyup", "setDirtyFlag()");

            Label lbl = (Label)e.Item.FindControl("lblAula");
            lbl.Text = "";

            listCData = cdataBo.GetCategoriaDatas();
            List<Requisicao> listReq = reqBo.GetRequisicoesPorAula(new Guid(lblAulaId.Text), cal);                        

            DateTime dataAtual = Convert.ToDateTime(lblData.Text);

            ddlAtividade.DataValueField = "Id";
            ddlAtividade.DataTextField = "Descricao";
            ddlAtividade.DataSource = listaAtividades;
            ddlAtividade.DataBind();

            ddlAtividade.SelectedValue = categorias[0].ToString();

            List<CategoriaRecurso> listCatRecursos = categoriaRecursoBo.GetCategoriaRecursoSortedByUse();
            // listCatRecursos.Sort();
            CategoriaRecurso dummy = new CategoriaRecurso(dummyGuid, "Selecionar...");
            listCatRecursos.Insert(0, dummy);

            string recursos = "";
            cbRecursos.Items.Clear();
            foreach (Requisicao r in listReq)
            {
                if (recursos != String.Empty) recursos += "<br/>";
                string descr = r.Prioridade + ": " + r.CategoriaRecurso.Descricao;
                cbRecursos.Items.Add(new ListItem(descr, r.IdRequisicao.ToString()));
                recursos += descr;
                listCatRecursos.Remove(listCatRecursos.Find(delegate(CategoriaRecurso cr)
                {
                    return cr.Descricao == r.CategoriaRecurso.Descricao;
                }
                ));             
            }

            DropDownList ddlCategoriaRecurso = (DropDownList)e.Item.FindControl("ddlRecurso");
            if (semRecursos)
            {
                dgAulas.Columns[8].Visible = false;
                dgAulas.Columns[9].Visible = false;
                dgAulas.Columns[10].Visible = false;
                butDel.Visible = false;
                //ddlCategoriaRecurso.Visible = false;
                //lblRecursosSelecionados.Visible = false;
            }
            else
            {
                ddlCategoriaRecurso.SelectedIndex = 0;
                ddlCategoriaRecurso.DataSource = listCatRecursos;
                ddlCategoriaRecurso.DataTextField = "Descricao";
                ddlCategoriaRecurso.DataValueField = "Id";
                ddlCategoriaRecurso.DataBind();
                butDel.Visible = true;
            }
            if (recursos == String.Empty)
                butDel.Visible = false;

//            ddlCategoriaRecurso.Items.Remove("Laborat�rio");

            lblRecursosSelecionados.Text = recursos;

            //Data data = null;
            //verifica as datas para pintar as linhas
            if ((dataAtual >= cal.InicioG2))
            {
                e.Item.BackColor = Color.LightGray;
            }
            else
            {
                Data data = VerificaData(dataAtual);
                if (data != null)
                {
                    foreach (CategoriaData c in listCData)
                        if (c.Id == data.Categoria.Id)
                            if (!c.DiaLetivo)
                            {
                                e.Item.BackColor = c.Cor;
                                e.Item.Enabled = false;
                                txtDescricao.Text = c.Descricao;
                                lblCorDaData.Text = "True";
								break;
                            }
                            else
                            {
								facin = (bool) Session["facin"];
								if(facin) {
									lblDescData.Text = c.Descricao;
									txtDescricao.Text = c.Descricao;// + " "+facin; // + " - " + txtDescricao.Text;
									//txtDescricao.Text = txtDescricao.Text;
									e.Item.BackColor = c.Cor;
									lblCorDaData.Text = "True";
								}
								else {
								    e.Item.BackColor = cor;								
									lblCorDaData.Text = "False";
								}
								lbl.Text = (cont++).ToString();
								break;
                            }
                }
                else
                {
                    e.Item.BackColor = cor;
                    lblCorDaData.Text = "False";
                    lbl.Text = (cont++).ToString();
                }
            }

            categorias.RemoveAt(0);
            argb.RemoveAt(0);
        }

    }

    protected void dgAulas_ItemCommand(object sender, DataGridCommandEventArgs e)
    {
        if (e.CommandName == "Select")
        {
            //salva dados digitados antes de selecionar os recursos
            Label lblData = (Label)e.Item.FindControl("lblData");
            Label lblHora = (Label)e.Item.FindControl("lblHora");
            TextBox txtDescricao = (TextBox)e.Item.FindControl("txtDescricao");
            DropDownList ddlAtividade = (DropDownList)e.Item.FindControl("ddlAtividade");
            Label lblCorDaData = (Label)e.Item.FindControl("lblCorDaData");
            Label lblDescData = (Label)e.Item.FindControl("lblDescData");
            Label lblaulaId = (Label)e.Item.FindControl("lblAulaId");

            Guid idaula = new Guid(lblaulaId.Text);
            Guid idturma = (Guid)Session["TurmaId"];
            Turma turma = turmaBo.GetTurmaById(idturma);

            string hora = lblHora.Text;
            DateTime data = Convert.ToDateTime(lblData.Text);

            string aux = txtDescricao.Text;
            string descricao = aux.Substring(aux.IndexOf('\n') + 1);

            Guid idcategoria = new Guid(ddlAtividade.SelectedValue);
            CategoriaAtividade categoria = categoriaBo.GetCategoriaAtividadeById(idcategoria);

            if (e.Item.BackColor != Color.LightGray && lblCorDaData.Text.Equals("False"))
                e.Item.BackColor = categoria.Cor;


            Aula aula = Aula.GetAula(idaula, turma, hora, data, descricao, categoria);

            aulaBo.UpdateAula(aula);

            txtDescricao.Text = lblDescData.Text + "\n" + descricao;
			//txtDescricao.Text = descricao;

            // abre a popup de selecao de recursos
            string id = lblaulaId.Text;
           
            ScriptManager.RegisterClientScriptBlock(this, GetType(), "onClick", "popitup('SelecaoRecursos.aspx?AulaId=" + id + "');", true);           
        }
        if (e.CommandName == "Salvar")
        {

            try
            {
                
                Label lblaulaId = (Label)e.Item.FindControl("lblAulaId");
                Label lblData = (Label)e.Item.FindControl("lblData");
                Label lblHora = (Label)e.Item.FindControl("lblHora");
                TextBox txtDescricao = (TextBox)e.Item.FindControl("txtDescricao");
                DropDownList ddlAtividade = (DropDownList)e.Item.FindControl("ddlAtividade");
                Label lblCorDaData = (Label)e.Item.FindControl("lblCorDaData");
                Label lblDescData = (Label)e.Item.FindControl("lblDescData");

                Guid idaula = new Guid(lblaulaId.Text);
                Guid idturma = (Guid)Session["TurmaId"];
                Turma turma = turmaBo.GetTurmaById(idturma);

                string hora = lblHora.Text;
                DateTime data = Convert.ToDateTime(lblData.Text);

                string aux = txtDescricao.Text;
                string descricao = aux.Substring(aux.IndexOf('\n') + 1);

                Guid idcategoria = new Guid(ddlAtividade.SelectedValue);
                CategoriaAtividade categoria = categoriaBo.GetCategoriaAtividadeById(idcategoria);

                if (e.Item.BackColor != Color.LightGray && lblCorDaData.Text.Equals("False"))
                    e.Item.BackColor = categoria.Cor;


                Aula aula = Aula.GetAula(idaula, turma, hora, data, descricao, categoria);

                aulaBo.UpdateAula(aula);

                //txtDescricao.Text = lblDescData.Text + "\n" + descricao;
				txtDescricao.Text = descricao;
                lblResultado.Text = "Altera��o realizada com sucesso!";

            }
            catch (Exception ex)
            {
                Response.Redirect("~/Default/Erro.aspx?Erro=" + ex.Message);
            }
        }
    }

    protected void btnSalvarTudo_Click(object sender, EventArgs e)
    {
        SalvarTodos();        
    }

    protected void SalvarTodos()
    {
        DataGridItemCollection t = dgAulas.Items;
        Label lblAulaId;
        Label lblAula;
        Label lblData;
        Label lblHora;
        Label lblCorDaData;
        TextBox txtDescricao;
        Label lblDescData;
        DropDownList ddlAtividade;
        string hora;
        string aux;
        string descricao;
        DateTime data;
        Guid idcategoria;
        Guid idaula;
        CategoriaAtividade categoria;
        Aula aula;

        Guid idturma = (Guid)Session["TurmaId"];
        Turma turma = turmaBo.GetTurmaById(idturma);

        for (int i = 0; i < t.Count; i++)
        {
            lblAulaId = (Label)t[i].FindControl("lblAulaId");
            lblAula = (Label)t[i].FindControl("lblAula");
            lblData = (Label)t[i].FindControl("lblData");
            lblHora = (Label)t[i].FindControl("lblHora");
            txtDescricao = (TextBox)t[i].FindControl("txtDescricao");
            ddlAtividade = (DropDownList)t[i].FindControl("ddlAtividade");
            lblCorDaData = (Label)t[i].FindControl("lblCorDaData");
            lblDescData = (Label)t[i].FindControl("lblDescData");

            idaula = new Guid(lblAulaId.Text);
            hora = lblHora.Text;
            data = Convert.ToDateTime(lblData.Text);
            aux = txtDescricao.Text.Trim();
            descricao = aux.Substring(aux.IndexOf('\n') + 1);

            idcategoria = new Guid(ddlAtividade.SelectedValue);
            categoria = categoriaBo.GetCategoriaAtividadeById(idcategoria);

            if (t[i].BackColor != Color.LightGray && lblCorDaData.Text.Equals("False"))
                t[i].BackColor = categoria.Cor;


            aula = Aula.GetAula(idaula, turma, hora, data, descricao, categoria);

            aulaBo.UpdateAula(aula);

            //txtDescricao.Text = lblDescData.Text + "\n" + descricao;
			txtDescricao.Text = descricao;
        }

        lblResultado.Text = "Altera��o realizada com sucesso!";
        ScriptManager.RegisterClientScriptBlock(this, GetType(), "OnClick",
                @"releaseDirtyFlag();", true);
    }


    protected void btnExportarHTML_Click(object sender, EventArgs e)
    {
        ExportarHtml();
    }

    protected void btnExportarCSV_Click(object sender, EventArgs e)
    {
        ExportarCSV();
    }

    protected void btnImportarCSV_Click(object sender, EventArgs e)
    {
        
        ImportarCSV();
    }

    private void ExportarHtml()
    {
        DataTable tabela = new DataTable();

		/*
		tabela.Columns.Add("#");
		tabela.Columns.Add("Dia");
		tabela.Columns.Add("Data");
		tabela.Columns.Add("Hora");
		tabela.Columns.Add("Descri��o");
		tabela.Columns.Add("Atividade");		
		tabela.Columns.Add("CorDaData");
		tabela.Columns.Add("Recursos");
		*/
				        
		foreach (DataGridColumn coluna in dgAulas.Columns)
        {
            tabela.Columns.Add(coluna.HeaderText);
        }
        tabela.Columns.Add("Recursos");

        DataRow dr;
        Label lblAux; 
        TextBox txtDescricao;
        DropDownList ddlAtividade;
        foreach(DataGridItem item in dgAulas.Items)
        {
            dr = tabela.NewRow();
            lblAux = (Label)item.FindControl("lblAula");
            dr["#"] = lblAux.Text;

			lblAux = (Label)item.FindControl("lblDia");
			dr["Dia"] = lblAux.Text;
			
			lblAux = (Label)item.FindControl("lblData");
			dr["Data"] = lblAux.Text;
			
			lblAux = (Label)item.FindControl("lblHora");
			dr["Hora"] = lblAux.Text;
			
            //lblAux = (Label)item.FindControl("lblData2");
            //dr["Data Hora"] = lblAux.Text;

            //lblAux = (Label)item.FindControl("lblDia2");
            //dr["Data Hora"] += " " + lblAux.Text;

            //lblAux = (Label)item.FindControl("lblHora");
            //dr["Data Hora"] += lblAux.Text;

            /*lblAux = (Label)item.FindControl("lblDia");
            dr["Dia"] = lblAux.Text;

            lblAux = (Label)item.FindControl("lblData");
            dr["Data"] = lblAux.Text;

            lblAux = (Label)item.FindControl("lblHora");
            dr["Hora"] = lblAux.Text;
            */

            txtDescricao = (TextBox)item.FindControl("txtDescricao");
            dr["Descri��o"] = txtDescricao.Text;

            ddlAtividade = (DropDownList)item.FindControl("ddlAtividade");
            dr["Atividade"] = ddlAtividade.SelectedItem.Text;

            lblAux = (Label)item.FindControl("lblRecursosSelecionados");
            dr["Recursos"] = lblAux.Text;

            dr["CorDaData"] = item.BackColor.Name;
            tabela.Rows.Add(dr);
        }

        
        Session["DownHtml"] = tabela;
        Response.Redirect("DownloadHtml2.aspx");
    }

    // Deleta o(s) recurso(s) selecionado(s)
    protected void butDeletar_Click(object sender, ImageClickEventArgs e)
    {
        ImageButton butDel = (ImageButton)sender;

        // O checkbox list est� dentro da c�lula da tabela...
        HtmlTableCell cell = (HtmlTableCell)butDel.Parent;
        CheckBoxList cbList = (CheckBoxList)cell.FindControl("cbRecursos");

        // Para chegar no DataGridItem correspondente... bleargh!
        DataGridItem grid = (DataGridItem)cell.Parent.Parent.Parent.Parent.Parent;

        DropDownList ddlRecurso = (DropDownList)grid.FindControl("ddlRecurso");
        string dataString = ((Label)grid.FindControl("lblData")).Text;
        DateTime data = Convert.ToDateTime(dataString);
        string horario = ((Label)grid.FindControl("lblHora")).Text;
        string aulaString = ((Label)grid.FindControl("lblAulaId")).Text;

        Guid aulaId = new Guid(aulaString);
        Aula aula = aulaBo.GetAulaById(aulaId);

        RequisicoesBO controleRequisicoes = new RequisicoesBO();
        CategoriaRecursoBO controladorCategorias = new CategoriaRecursoBO();

        // Varre o checkbox list do fim para o in�cio,
        // e remove todos os recursos selecionados (da tela e do BD)

        // Se so houver um recurso na lista, remove mesmo sem selecionar
        if (cbList.Items.Count == 1)
            cbList.Items[0].Selected = true;
        for (int r = cbList.Items.Count-1; r >=0; r--)
        {
            ListItem recurso = cbList.Items[r];
            if (recurso.Selected)
            {
                controleRequisicoes.DeletaRequisicao(new Guid(recurso.Value));
                cbList.Items.RemoveAt(r);
            }
        }

        if (cbList.Items.Count == 0)
            butDel.Visible = false;

        List<Requisicao> requisicoesExistentes = controleRequisicoes.GetRequisicoesPorAula(aulaId, cal);
        var reqs = from req in requisicoesExistentes
                   orderby req.Prioridade
                   select req;

        List<CategoriaRecurso> listCatRecursos = categoriaRecursoBo.GetCategoriaRecursoSortedByUse();
        CategoriaRecurso dummy = new CategoriaRecurso(dummyGuid, "Selecionar...");
        listCatRecursos.Insert(0, dummy);

        int pri = 1;
        cbList.Items.Clear();
        foreach (Requisicao req in reqs)
        {
            req.Prioridade = pri++;
            controleRequisicoes.UpdateRequisicoes(req);
            cbList.Items.Add(new ListItem(req.Prioridade + ": " + req.CategoriaRecurso.Descricao,req.IdRequisicao.ToString()));
            listCatRecursos.Remove(listCatRecursos.Find(delegate (CategoriaRecurso cr)
            {
                return cr.Descricao == req.CategoriaRecurso.Descricao;
            }
            ));
        }
        ddlRecurso.SelectedIndex = 0;
        ddlRecurso.DataSource = listCatRecursos;
        ddlRecurso.DataBind();
    }

    protected void ddlRecurso_SelectedIndexChanged(object sender, EventArgs e)
    {
        DropDownList ddlRecurso = (DropDownList) sender;
        string recString = ddlRecurso.SelectedValue;

        TableCell cell = (TableCell) ddlRecurso.Parent;
        DataGridItem gridItem = (DataGridItem) cell.Parent;
        ImageButton butDel = (ImageButton)gridItem.FindControl("butDeletar");

        // Salva dados digitados

        SalvarTodos();
//        SalvaDados(gridItem);
        
        // abre a popup de selecao de recursos
        //string id = lblaulaId.Text;
        //ScriptManager.RegisterClientScriptBlock(this, GetType(), "onClick", "popitup('SelecaoRecursos.aspx?AulaId=" + id + "');", true);

        Label lblaulaId = (Label) gridItem.FindControl("lblAulaId");
        Guid idAula = new Guid(lblaulaId.Text);
        Aula aulaAtual = aulaBo.GetAulaById(idAula);

        RequisicoesBO controleRequisicoes = new RequisicoesBO();
        IList<Requisicao> requisicoesExistentes = controleRequisicoes.GetRequisicoesPorAula(idAula, cal);
        int pri = 0;
        foreach (Requisicao req in requisicoesExistentes)
            if (req.Prioridade > pri)
                pri = req.Prioridade;

        CategoriaRecursoBO controladorCategorias = new CategoriaRecursoBO();
        Guid catId = new Guid(ddlRecurso.SelectedValue);
        CategoriaRecurso categoria = controladorCategorias.GetCategoriaRecursoById(catId);
        Requisicao novaReq = Requisicao.NewRequisicao(aulaAtual, categoria, pri+1); // teste! sempre prioridade + 1

        // Insere a nova requisi��o
        controleRequisicoes.InsereRequisicao(novaReq);
        requisicoesExistentes.Add(novaReq);

        // Atualiza label com os recursos selecionados
        Label lblRecursosSelecionados = (Label) gridItem.FindControl("lblRecursosSelecionados");
        CheckBoxList cbRecursos = (CheckBoxList)gridItem.FindControl("cbRecursos");
        string recursos = "";
        cbRecursos.Items.Clear();
        foreach (Requisicao r in requisicoesExistentes)
        {
            if (recursos != String.Empty) recursos += "<br/>";
            string descr = r.Prioridade + ": " + r.CategoriaRecurso.Descricao;
            recursos += descr;
            cbRecursos.Items.Add(new ListItem(descr, r.IdRequisicao.ToString()));
        }
        lblRecursosSelecionados.Text = recursos;

        // Remove a categoria selecionada do drop down list
        ddlRecurso.Items.Remove(ddlRecurso.Items.FindByValue(ddlRecurso.SelectedValue));
        ddlRecurso.SelectedIndex = 0;
        butDel.Visible = true;
    }

    // Troca de tipo de atividade, atualiza aula e cores na tela
    protected void ddlAtividade_SelectedIndexChanged(object sender, EventArgs e)
    {
        DropDownList ddlAtividade = (DropDownList)sender;
        string ativString = ddlAtividade.SelectedValue;

        TableCell cell = (TableCell)ddlAtividade.Parent;
        DataGridItem gridItem = (DataGridItem)cell.Parent;

        // Salva dados digitados

        SalvarTodos();
//        SalvaDados(gridItem);
    }

    // Salva os dados da linha corrente (chamados pelos eventos de select das drop down lists, etc)
    private void SalvaDados(DataGridItem gridItem)
    {
        // Salva dados digitados

        Label lblData = (Label)gridItem.FindControl("lblData");
        Label lblHora = (Label)gridItem.FindControl("lblHora");
        TextBox txtDescricao = (TextBox)gridItem.FindControl("txtDescricao");

        DropDownList ddlAtividade = (DropDownList)gridItem.FindControl("ddlAtividade");
        Label lblCorDaData = (Label)gridItem.FindControl("lblCorDaData");
        Label lblDescData = (Label)gridItem.FindControl("lblDescData");
        Label lblaulaId = (Label)gridItem.FindControl("lblAulaId");

        Guid idaula = new Guid(lblaulaId.Text);
        Guid idturma = (Guid)Session["TurmaId"];
        Turma turma = turmaBo.GetTurmaById(idturma);

        string hora = lblHora.Text;
        DateTime data = Convert.ToDateTime(lblData.Text);

        string aux = txtDescricao.Text;
        string descricao = aux.Substring(aux.IndexOf('\n') + 1);

        Guid idcategoria = new Guid(ddlAtividade.SelectedValue);
        CategoriaAtividade categoria = categoriaBo.GetCategoriaAtividadeById(idcategoria);

        if (gridItem.BackColor != Color.LightGray && lblCorDaData.Text.Equals("False"))
            gridItem.BackColor = categoria.Cor;

        Aula aula = Aula.GetAula(idaula, turma, hora, data, descricao, categoria);
        
        aulaBo.UpdateAula(aula);        

        //txtDescricao.Text = lblDescData.Text + "\n" + descricao;
		txtDescricao.Text = descricao;
    }

    protected void ExportarCSV()
    {
        DataTable tabela = new DataTable();

        foreach (DataGridColumn coluna in dgAulas.Columns)
        {
            tabela.Columns.Add(coluna.HeaderText);
        }

        DataRow dr;
        Label lblAux;
        TextBox txtDescricao;
        DropDownList ddlAtividade;
        foreach (DataGridItem item in dgAulas.Items)
        {
            dr = tabela.NewRow();
            lblAux = (Label)item.FindControl("lblAula");
            dr["#"] = lblAux.Text;

            lblAux = (Label)item.FindControl("lblDia");
            dr["Dia"] = lblAux.Text;

            lblAux = (Label)item.FindControl("lblData");
            dr["Data"] = lblAux.Text;

            lblAux = (Label)item.FindControl("lblHora");
            dr["Hora"] = lblAux.Text;

            txtDescricao = (TextBox)item.FindControl("txtDescricao");
            dr["Descri��o"] = txtDescricao.Text;

            ddlAtividade = (DropDownList)item.FindControl("ddlAtividade");
            dr["Atividade"] = ddlAtividade.SelectedItem.Text;

            dr["CorDaData"] = item.BackColor.Name;
            tabela.Rows.Add(dr);
        }
        Session["DownCSV"] = tabela;
        Response.Redirect("DownloadCSV2.aspx");
    }

    protected void ImportarCSV()
    {
        if (csvUpload.HasFile)
        {
            string folderPath = "c:\\temp";
//            string folderPath = Server.MapPath("~/AppData");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string pathname = folderPath + "\\" + Path.GetFileName(csvUpload.FileName);
            Debug.WriteLine("Save pathname: "+pathname);
            csvUpload.SaveAs(pathname);
            
            DataTable dt = ConvertCSVtoDataTable(pathname);
            if(dt == null)
            {
                MessageBox("Formato de arquivo inv�lido (deve ser CSV do sistema de atas)");
                return;
            }

            int cont = 0;
            foreach (DataGridItem item in dgAulas.Items)
            {
                DataRow dr = dt.Rows[cont];

                Label lblAux = (Label)item.FindControl("lblAula");
                if (lblAux.Text == String.Empty) continue;

                string corData = item.BackColor.Name;
                if (corData == "LightGray")
                    break;

                TextBox txtDescricao = (TextBox)item.FindControl("txtDescricao");
                txtDescricao.Text = (string) dr[6];
                Debug.WriteLine(cont + ": " + txtDescricao.Text);

                DropDownList ddlAtividade = (DropDownList)item.FindControl("ddlAtividade");
                string atividade = (string) dr[5];
                int tipoAula = 0;
                switch (atividade)
                {
                    case "2": tipoAula = 4; break;
                    case "4": tipoAula = 6; break;
                    case "6": tipoAula = 0; break;
                    case "7": tipoAula = 8; break;
                }
                ddlAtividade.SelectedIndex = tipoAula;
                cont++;
                if (cont >= dt.Rows.Count)
                    break;
            }
            SalvarTodos();
            File.Delete(pathname);

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            string message = "Total: " + cont + " de " + dt.Rows.Count;
            MessageBox(message);
        }
        else
        {
            MessageBox("Primeiro selecione um arquivo CSV");
        }
    }

    protected void MessageBox(string message)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("<script type = 'text/javascript'>");
        sb.Append("window.onload=function(){");
        sb.Append("alert('");
        sb.Append(message);
        sb.Append("')};");
        sb.Append("</script>");

        ClientScript.RegisterClientScriptBlock(this.GetType(), "alert", sb.ToString());
    }

    protected DataTable ConvertCSVtoDataTable(string strFilePath)
    {
        DataTable dt = new DataTable();
        using (StreamReader sr = new StreamReader(strFilePath))
        {
            string[] headers = sr.ReadLine().Split(';');
            if (headers[0] != "cdDisciplinaOrigem")
                return null;
            foreach (string header in headers)
            {
                dt.Columns.Add(header);
            }
            while (!sr.EndOfStream)
            {
                string[] rows = sr.ReadLine().Split(';');
                DataRow dr = dt.NewRow();
                for (int i = 0; i < headers.Length; i++)
                {
                    dr[i] = rows[i];
                }
                dt.Rows.Add(dr);
            }
        }
        return dt;
    }
}
