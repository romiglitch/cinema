<%@ Page Title="" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="MovieEditor.aspx.cs" Inherits="Shipping.MovieEditor" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="https://fonts.googleapis.com/css2?family=Montserrat:wght@500;600&display=swap" rel="stylesheet">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">


    <div class="signup-form" style=" margin-top: 80px;">

        <h1>שלום מנהל</h1>
        <%--חצים--%>
        <div style="text-align: center; margin-top: 20px;">
            <asp:Button  ID="Button1" runat="server" CssClass="page-btn" Text="&#8592;" OnClick="btnPrev_Click" />
            <asp:Label ID="lblPageNumber1" runat="server" CssClass="page-number-label" Text="Page 1" />
            <asp:Button ID="Button2" runat="server" CssClass="page-btn" Text="&#8594;" OnClick="btnNext_Click" />
        </div>
        <asp:DataList ID="DLMovies" runat="server" RepeatDirection="Horizontal" RepeatColumns="2" DataKeyField="Id" BackColor="White" BorderColor="#E7E7FF" BorderStyle="None" BorderWidth="1px" GridLines="Horizontal" OnEditCommand="DLMovies_EditCommand"
            OnCancelCommand="DLMovies_CancelCommand"
            OnUpdateCommand="DLMovies_UpdateCommand" OnItemDataBound="DLMovies_ItemDataBound">



            <AlternatingItemStyle BackColor="#F7F7F7" />
            <FooterStyle BackColor="#B5C7DE" ForeColor="#4A3C8C" />
            <HeaderStyle BackColor="#4A3C8C" Font-Bold="True" ForeColor="#F7F7F7" />
            <ItemStyle BackColor="#E7E7FF" ForeColor="#4A3C8C" />
            <SelectedItemStyle BackColor="#738A9C" Font-Bold="True" ForeColor="#F7F7F7" />
            <ItemTemplate>
                <div style="height: 620px; width: 500px; border: 1px solid #ccc; padding: 10px;">
                    <span style="color: Blue">Id:</span> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;              
                 <asp:Label ID="LblId" runat="server"
                     Text='<%# Eval("Id") %>'>                                                                   
                 </asp:Label>
                    <br />
                    <span style="color: Blue">Title:</span> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;              
 <asp:Label ID="LblTitle" runat="server"
     Text='<%# Eval("Title") %>'>                                                                   
 </asp:Label>
                    <br />
                    <span style="color: Blue">Description:</span> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;              
 <asp:Label ID="LblDesc" runat="server"
     Text='<%# Eval("Description") %>'>                                                                   
 </asp:Label>
                    <br />
                    <span style="color: Blue">Duration:</span> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;              
 <asp:Label ID="LblDur" runat="server"
     Text='<%# Eval("Duration") %>'>                                                                   
 </asp:Label>
                    <br />
                    <span style="color: Blue">Age:</span> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;              
 <asp:Label ID="LblAge" runat="server"
     Text='<%# Eval("Age") %>'>                                                                   
 </asp:Label>
                    <br />
                    <span style="color: Blue">Poster:</span> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;  
                     <asp:Image ID="Img" CssClass="poster-img" runat="server"
           ImageUrl='<%# string.IsNullOrEmpty(Eval("Poster").ToString()) 
                     ? "/posters/no-image.jpg" 
                     : Eval("Poster") %>' />


                    <br />
                    <asp:LinkButton runat="server" ID="BtnEdit" CssClass="clean-black-btn" CommandName="edit" Text="Edit" />
                </div>
            </ItemTemplate>
            <EditItemTemplate>
                <div style="height: 520px; width: 520px; border: 1px solid #ccc; padding: 10px;">
                    <span style="color: Blue">Id:</span>                &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;                    
                <asp:Label ID="LblId" runat="server" Text='<%# Eval("Id") %>'>                     </asp:Label>
                    <br />
                    <br />
                    <span style="color: Blue">Title:</span>
                    <asp:TextBox ID="TxtTitle" runat="server" Text='<%# Eval("Title") %>'>                     </asp:TextBox>
                    <br />
                    <br />
                    <span style="color: Blue">Description:</span>&nbsp;                        
                <asp:TextBox ID="TxtDesc" runat="server" Text='<%# Eval("Description") %>'>                     </asp:TextBox>
                    <br />
                    <br />
                    <span style="color: Blue">Duration:</span> &nbsp;            
                <asp:TextBox ID="TxtDur" runat="server"
                    Text='<%# Eval("Duration") %>'>                                                                   
                </asp:TextBox>
                    <br />
                    <br />
                    <span style="color: Blue">Age:</span> &nbsp;            
                <asp:TextBox ID="TxtAge" runat="server"
                    Text='<%# Eval("Age") %>'>                                                                   
                </asp:TextBox>
                    <br />
                    <br />
                    <span style="color: Blue">Poster:</span> &nbsp;
                    <asp:Image ID="Img"  CssClass="poster-img-edit" runat="server"
           ImageUrl='<%# string.IsNullOrEmpty(Eval("Poster").ToString()) 
                     ? "/posters/no-image.jpg" 
                     : Eval("Poster") %>' />
                    <br />
                    <asp:LinkButton runat="server" ID="LinkButton2" CssClass="clean-black-btn" CommandName="cancel" Text="Cancel" />
                    <asp:LinkButton runat="server" ID="LinkButton3" CssClass="clean-black-btn" CommandName="update" Text="Update" />


                </div>
            </EditItemTemplate>
        </asp:DataList>
        <%--חצים--%>
        <div style="text-align: center; margin-top:20px;">
            <asp:Button ID="btnPrev" runat="server" CssClass="page-btn" Text="&#8592;" OnClick="btnPrev_Click" />
            <asp:Label ID="lblPageNumber2" runat="server" CssClass="page-number-label" Text="Page 1" />
            <asp:Button ID="btnNext" runat="server" CssClass="page-btn" Text="&#8594;" OnClick="btnNext_Click" />

        </div>

    </div>
</asp:Content>
