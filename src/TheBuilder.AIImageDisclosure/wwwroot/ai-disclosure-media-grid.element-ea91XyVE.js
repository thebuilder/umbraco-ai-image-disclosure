import { UMB_MEDIA_DETAIL_STORE_CONTEXT as z, UMB_EDIT_MEDIA_WORKSPACE_PATH_PATTERN as N, UMB_MEDIA_PLACEHOLDER_ENTITY_TYPE as V } from "@umbraco-cms/backoffice/media";
import { MediaService as G } from "@umbraco-cms/backoffice/external/backend-api";
import { tryExecute as F } from "@umbraco-cms/backoffice/resources";
import { UMB_COLLECTION_CONTEXT as X } from "@umbraco-cms/backoffice/collection";
import { repeat as Y, html as _, ifDefined as p, css as K, state as y, customElement as J } from "@umbraco-cms/backoffice/external/lit";
import { UmbFileDropzoneItemStatus as B } from "@umbraco-cms/backoffice/dropzone";
import { UmbTextStyles as Q } from "@umbraco-cms/backoffice/style";
import { UmbLitElement as j } from "@umbraco-cms/backoffice/lit-element";
import "@umbraco-cms/backoffice/imaging";
const e1 = "data:image/svg+xml,%3c?xml%20version='1.0'%20encoding='UTF-8'?%3e%3csvg%20id='Calque_1'%20data-name='Calque%201'%20xmlns='http://www.w3.org/2000/svg'%20viewBox='0%200%201789.84%20566.93'%3e%3cdefs%3e%3cstyle%3e%20.cls-1%20{%20fill-rule:%20evenodd;%20}%20.cls-2%20{%20fill:%20%23fff;%20}%20%3c/style%3e%3c/defs%3e%3cpath%20class='cls-1'%20d='M352.84,410.77h1093.16c85.55,0,145.54-59.94,145.54-133.2h0c0-73.27-59.99-133.21-145.54-133.21H352.84c-85.55,0-145.54,59.94-145.54,133.2h0c0,73.26,64.99,133.2,150.54,133.2'/%3e%3cg%3e%3cpath%20class='cls-2'%20d='M326.09,341c-.98,0-1.87-.4-2.66-1.19-.8-.8-1.19-1.68-1.19-2.66,0-.61.06-1.16.18-1.65l43.18-117.96c.37-1.35,1.13-2.54,2.3-3.58,1.16-1.04,2.79-1.56,4.87-1.56h27.19c2.08,0,3.7.52,4.87,1.56,1.16,1.04,1.93,2.24,2.3,3.58l42.99,117.96c.24.49.37,1.04.37,1.65,0,.98-.4,1.87-1.19,2.66-.8.8-1.75,1.19-2.85,1.19h-22.6c-1.84,0-3.22-.46-4.13-1.38-.92-.92-1.5-1.75-1.75-2.48l-7.17-18.74h-49.06l-6.98,18.74c-.25.73-.8,1.56-1.65,2.48-.86.92-2.33,1.38-4.41,1.38h-22.6ZM369.27,291.94h34.18l-17.27-48.51-16.9,48.51Z'/%3e%3cpath%20class='cls-2'%20d='M469.58,341c-1.35,0-2.45-.43-3.31-1.29-.86-.86-1.29-1.96-1.29-3.31v-119.43c0-1.35.43-2.45,1.29-3.31s1.96-1.29,3.31-1.29h24.62c1.35,0,2.45.43,3.31,1.29.86.86,1.29,1.96,1.29,3.31v119.43c0,1.35-.43,2.45-1.29,3.31s-1.96,1.29-3.31,1.29h-24.62Z'/%3e%3c/g%3e%3cg%3e%3cpath%20class='cls-2'%20d='M639.25,325.87c-8.43,0-15.63-1.41-21.59-4.24-5.97-2.83-10.58-6.95-13.86-12.38-3.27-5.43-5.04-11.99-5.31-19.71-.09-3.85-.13-7.98-.13-12.38s.04-8.56.13-12.51c.27-7.53,2.04-13.94,5.31-19.24,3.27-5.29,7.94-9.35,13.99-12.17,6.05-2.83,13.2-4.24,21.46-4.24,6.64,0,12.46.85,17.49,2.56,5.02,1.7,9.24,3.92,12.65,6.66,3.41,2.74,6.01,5.72,7.8,8.95,1.79,3.23,2.74,6.32,2.83,9.28,0,.81-.27,1.5-.81,2.08-.54.58-1.26.87-2.15.87h-16.68c-.9,0-1.59-.18-2.08-.54-.49-.36-.92-.9-1.28-1.61-.63-1.61-1.64-3.25-3.03-4.91-1.39-1.66-3.25-3.07-5.58-4.24-2.33-1.16-5.38-1.75-9.15-1.75-5.65,0-10.11,1.48-13.39,4.44-3.27,2.96-5.04,7.8-5.31,14.53-.27,7.71-.27,15.56,0,23.54.27,7,2.08,12.02,5.45,15.07,3.36,3.05,7.87,4.57,13.52,4.57,3.68,0,6.97-.65,9.89-1.95,2.91-1.3,5.22-3.34,6.93-6.12,1.7-2.78,2.56-6.32,2.56-10.63v-3.36h-14.66c-.9,0-1.66-.34-2.29-1.01-.63-.67-.94-1.46-.94-2.35v-8.61c0-.99.31-1.79.94-2.42.63-.63,1.39-.94,2.29-.94h32.96c.99,0,1.79.31,2.42.94.63.63.94,1.44.94,2.42v14.66c0,7.71-1.68,14.31-5.04,19.77-3.36,5.47-8.16,9.66-14.39,12.58-6.23,2.92-13.52,4.37-21.86,4.37Z'/%3e%3cpath%20class='cls-2'%20d='M710.73,324.52c-.99,0-1.79-.31-2.42-.94-.63-.63-.94-1.43-.94-2.42v-87.44c0-.99.31-1.79.94-2.42.63-.63,1.43-.94,2.42-.94h60.94c.99,0,1.79.31,2.42.94.63.63.94,1.44.94,2.42v10.76c0,.9-.32,1.66-.94,2.29-.63.63-1.44.94-2.42.94h-43.59v21.12h40.63c.99,0,1.79.32,2.42.94.63.63.94,1.44.94,2.42v9.95c0,.9-.32,1.66-.94,2.29s-1.44.94-2.42.94h-40.63v21.79h44.66c.99,0,1.79.32,2.42.94.63.63.94,1.44.94,2.42v10.63c0,.99-.32,1.8-.94,2.42s-1.44.94-2.42.94h-62.02Z'/%3e%3cpath%20class='cls-2'%20d='M805.88,324.52c-.99,0-1.79-.31-2.42-.94-.63-.63-.94-1.43-.94-2.42v-87.44c0-.99.31-1.79.94-2.42.63-.63,1.43-.94,2.42-.94h12.24c1.34,0,2.33.31,2.96.94.63.63,1.03,1.12,1.21,1.48l35.92,56.1v-55.15c0-.99.31-1.79.94-2.42.63-.63,1.39-.94,2.29-.94h13.72c.99,0,1.79.31,2.42.94.63.63.94,1.44.94,2.42v87.44c0,.9-.32,1.68-.94,2.35-.63.67-1.44,1.01-2.42,1.01h-12.38c-1.35,0-2.31-.34-2.89-1.01-.58-.67-1.01-1.14-1.28-1.41l-35.78-54.35v53.41c0,.99-.32,1.8-.94,2.42s-1.44.94-2.42.94h-13.59Z'/%3e%3cpath%20class='cls-2'%20d='M912.19,324.52c-.99,0-1.79-.31-2.42-.94-.63-.63-.94-1.43-.94-2.42v-87.44c0-.99.31-1.79.94-2.42.63-.63,1.43-.94,2.42-.94h60.94c.99,0,1.79.31,2.42.94.63.63.94,1.44.94,2.42v10.76c0,.9-.32,1.66-.94,2.29-.63.63-1.44.94-2.42.94h-43.59v21.12h40.63c.99,0,1.79.32,2.42.94.63.63.94,1.44.94,2.42v9.95c0,.9-.32,1.66-.94,2.29s-1.44.94-2.42.94h-40.63v21.79h44.66c.99,0,1.79.32,2.42.94.63.63.94,1.44.94,2.42v10.63c0,.99-.32,1.8-.94,2.42s-1.44.94-2.42.94h-62.02Z'/%3e%3cpath%20class='cls-2'%20d='M1007.35,324.52c-.99,0-1.79-.31-2.42-.94-.63-.63-.94-1.43-.94-2.42v-87.44c0-.99.31-1.79.94-2.42.63-.63,1.43-.94,2.42-.94h35.65c11.21,0,20.04,2.58,26.5,7.73,6.46,5.16,9.69,12.58,9.69,22.26,0,6.55-1.57,12.02-4.71,16.41-3.14,4.4-7.35,7.67-12.65,9.82l18.97,33.63c.27.54.4,1.03.4,1.48,0,.72-.27,1.37-.81,1.95-.54.58-1.21.87-2.02.87h-14.93c-1.61,0-2.82-.42-3.63-1.28-.81-.85-1.39-1.64-1.75-2.35l-16.41-30.81h-16.28v31.07c0,.99-.32,1.8-.94,2.42s-1.44.94-2.42.94h-14.66ZM1025.38,273h17.22c4.93,0,8.59-1.12,10.96-3.36,2.38-2.24,3.56-5.38,3.56-9.42s-1.17-7.22-3.5-9.55c-2.33-2.33-6.01-3.5-11.03-3.5h-17.22v25.83Z'/%3e%3cpath%20class='cls-2'%20d='M1100.62,324.52c-.81,0-1.48-.29-2.02-.87-.54-.58-.81-1.23-.81-1.95,0-.45.04-.85.13-1.21l32.02-86.5c.27-.99.81-1.84,1.61-2.56.81-.72,1.93-1.08,3.36-1.08h17.22c1.43,0,2.56.36,3.36,1.08.81.72,1.35,1.57,1.61,2.56l32.02,86.5c.09.36.13.76.13,1.21,0,.72-.27,1.37-.81,1.95-.54.58-1.21.87-2.02.87h-13.99c-1.35,0-2.33-.31-2.96-.94-.63-.63-1.03-1.21-1.21-1.75l-5.78-15.07h-37.94l-5.78,15.07c-.18.54-.58,1.12-1.21,1.75-.63.63-1.61.94-2.96.94h-13.99ZM1129.54,289.41h27.85l-13.86-38.74-13.99,38.74Z'/%3e%3cpath%20class='cls-2'%20d='M1225.64,324.52c-.99,0-1.79-.31-2.42-.94-.63-.63-.94-1.43-.94-2.42v-72.1h-24.48c-.9,0-1.66-.31-2.29-.94-.63-.63-.94-1.39-.94-2.29v-12.11c0-.99.31-1.79.94-2.42.63-.63,1.39-.94,2.29-.94h70.49c.99,0,1.79.31,2.42.94.63.63.94,1.44.94,2.42v12.11c0,.9-.32,1.66-.94,2.29-.63.63-1.44.94-2.42.94h-24.35v72.1c0,.99-.32,1.8-.94,2.42s-1.44.94-2.42.94h-14.93Z'/%3e%3cpath%20class='cls-2'%20d='M1298.32,324.52c-.99,0-1.79-.31-2.42-.94-.63-.63-.94-1.43-.94-2.42v-87.44c0-.99.31-1.79.94-2.42.63-.63,1.43-.94,2.42-.94h60.94c.99,0,1.79.31,2.42.94.63.63.94,1.44.94,2.42v10.76c0,.9-.32,1.66-.94,2.29-.63.63-1.44.94-2.42.94h-43.59v21.12h40.63c.99,0,1.79.32,2.42.94.63.63.94,1.44.94,2.42v9.95c0,.9-.32,1.66-.94,2.29s-1.44.94-2.42.94h-40.63v21.79h44.66c.99,0,1.79.32,2.42.94.63.63.94,1.44.94,2.42v10.63c0,.99-.32,1.8-.94,2.42s-1.44.94-2.42.94h-62.02Z'/%3e%3cpath%20class='cls-2'%20d='M1393.48,324.52c-.99,0-1.79-.31-2.42-.94-.63-.63-.94-1.43-.94-2.42v-87.44c0-.99.31-1.79.94-2.42.63-.63,1.43-.94,2.42-.94h33.77c8.97,0,16.46,1.41,22.46,4.24,6.01,2.83,10.6,6.95,13.79,12.38,3.18,5.43,4.86,12.13,5.04,20.11.09,3.95.13,7.4.13,10.36s-.05,6.37-.13,10.22c-.27,8.34-1.93,15.25-4.98,20.72-3.05,5.47-7.53,9.53-13.45,12.17-5.92,2.65-13.32,3.97-22.2,3.97h-34.44ZM1411.51,307.17h15.74c4.48,0,8.16-.67,11.03-2.02,2.87-1.35,5-3.47,6.39-6.39,1.39-2.91,2.13-6.7,2.22-11.37.09-2.6.16-4.93.2-7,.04-2.06.04-4.12,0-6.19-.05-2.06-.11-4.35-.2-6.86-.18-6.73-1.91-11.68-5.18-14.86-3.27-3.18-8.32-4.78-15.13-4.78h-15.07v59.46Z'/%3e%3c/g%3e%3c/svg%3e", s1 = "data:image/svg+xml,%3c?xml%20version='1.0'%20encoding='UTF-8'?%3e%3csvg%20id='Calque_1'%20data-name='Calque%201'%20xmlns='http://www.w3.org/2000/svg'%20viewBox='0%200%201700.79%20566.93'%3e%3cdefs%3e%3cstyle%3e%20.cls-1%20{%20fill-rule:%20evenodd;%20}%20.cls-2%20{%20fill:%20%23fff;%20}%20%3c/style%3e%3c/defs%3e%3cpath%20class='cls-1'%20d='M376.65,410.77h939.48c85.55,0,145.54-59.94,145.54-133.2h0c0-73.27-59.99-133.21-145.54-133.21H381.65c-85.55,0-150.54,59.94-150.54,133.2h0c0,73.26,64.99,133.2,150.54,133.2'/%3e%3cg%3e%3cpath%20class='cls-2'%20d='M626.46,325.63c-.92,0-1.72-.32-2.4-.96-.69-.64-1.03-1.46-1.03-2.47v-89.25c0-1.01.34-1.83,1.03-2.47.69-.64,1.49-.96,2.4-.96h12.77c1.37,0,2.4.37,3.09,1.1.69.73,1.17,1.28,1.44,1.65l25.27,46.55,25.54-46.55c.18-.36.62-.91,1.3-1.65.69-.73,1.72-1.1,3.09-1.1h12.77c1.01,0,1.83.32,2.47.96.64.64.96,1.47.96,2.47v89.25c0,1.01-.32,1.83-.96,2.47s-1.47.96-2.47.96h-14.01c-.92,0-1.69-.32-2.33-.96-.64-.64-.96-1.46-.96-2.47v-55.48l-17.44,32.68c-.46.82-1.05,1.56-1.79,2.2-.73.64-1.69.96-2.88.96h-6.45c-1.19,0-2.15-.32-2.88-.96-.73-.64-1.33-1.37-1.79-2.2l-17.44-32.68v55.48c0,1.01-.32,1.83-.96,2.47-.64.64-1.42.96-2.33.96h-14.01Z'/%3e%3cpath%20class='cls-2'%20d='M784.28,327c-8.33,0-15.52-1.37-21.56-4.12-6.04-2.75-10.76-6.89-14.14-12.43-3.39-5.54-5.22-12.52-5.49-20.94-.09-3.94-.14-7.85-.14-11.74s.04-7.85.14-11.88c.27-8.24,2.13-15.17,5.56-20.8,3.43-5.63,8.19-9.86,14.28-12.7,6.09-2.84,13.2-4.26,21.35-4.26s15.13,1.42,21.21,4.26c6.09,2.84,10.87,7.07,14.35,12.7,3.48,5.63,5.31,12.56,5.49,20.8.18,4.03.28,7.99.28,11.88s-.09,7.81-.28,11.74c-.27,8.42-2.11,15.4-5.49,20.94-3.39,5.54-8.1,9.68-14.14,12.43-6.04,2.75-13.18,4.12-21.42,4.12ZM784.28,309.29c5.31,0,9.68-1.62,13.11-4.88,3.43-3.25,5.24-8.44,5.42-15.58.18-4.03.27-7.8.27-11.33s-.09-7.25-.27-11.19c-.09-4.76-.96-8.65-2.61-11.67-1.65-3.02-3.82-5.24-6.52-6.66-2.7-1.42-5.84-2.13-9.41-2.13s-6.73.71-9.47,2.13c-2.75,1.42-4.92,3.64-6.52,6.66-1.6,3.02-2.5,6.91-2.68,11.67-.09,3.94-.14,7.67-.14,11.19s.04,7.3.14,11.33c.27,7.14,2.1,12.34,5.49,15.58,3.39,3.25,7.78,4.88,13.18,4.88Z'/%3e%3cpath%20class='cls-2'%20d='M856.69,325.63c-1.01,0-1.83-.32-2.47-.96-.64-.64-.96-1.46-.96-2.47v-89.25c0-1.01.32-1.83.96-2.47.64-.64,1.46-.96,2.47-.96h34.47c9.15,0,16.8,1.44,22.93,4.33,6.13,2.88,10.82,7.1,14.07,12.63,3.25,5.54,4.96,12.38,5.15,20.53.09,4.03.14,7.55.14,10.57s-.05,6.5-.14,10.44c-.27,8.51-1.97,15.56-5.08,21.15-3.11,5.58-7.69,9.73-13.73,12.43-6.04,2.7-13.59,4.05-22.66,4.05h-35.15ZM875.09,307.92h16.07c4.58,0,8.33-.69,11.26-2.06,2.93-1.37,5.1-3.55,6.52-6.52,1.42-2.97,2.17-6.84,2.27-11.6.09-2.65.16-5.03.21-7.14.04-2.1.04-4.21,0-6.32-.05-2.1-.12-4.44-.21-7-.18-6.87-1.95-11.92-5.29-15.17-3.34-3.25-8.49-4.87-15.45-4.87h-15.38v60.69Z'/%3e%3cpath%20class='cls-2'%20d='M964.67,325.63c-1.01,0-1.83-.32-2.47-.96-.64-.64-.96-1.46-.96-2.47v-89.25c0-1.01.32-1.83.96-2.47.64-.64,1.46-.96,2.47-.96h15.52c1.01,0,1.83.32,2.47.96.64.64.96,1.47.96,2.47v89.25c0,1.01-.32,1.83-.96,2.47-.64.64-1.46.96-2.47.96h-15.52Z'/%3e%3cpath%20class='cls-2'%20d='M1018,325.63c-1.01,0-1.83-.32-2.47-.96-.64-.64-.96-1.46-.96-2.47v-89.25c0-1.01.32-1.83.96-2.47.64-.64,1.46-.96,2.47-.96h61.24c1.01,0,1.83.32,2.47.96.64.64.96,1.47.96,2.47v11.67c0,1.01-.32,1.83-.96,2.47-.64.64-1.47.96-2.47.96h-43.25v23.21h40.51c1.01,0,1.83.32,2.47.96.64.64.96,1.47.96,2.47v11.67c0,.92-.32,1.69-.96,2.33-.64.64-1.46.96-2.47.96h-40.51v32.54c0,1.01-.32,1.83-.96,2.47-.64.64-1.46.96-2.47.96h-14.56Z'/%3e%3cpath%20class='cls-2'%20d='M1112.25,325.63c-1.01,0-1.83-.32-2.47-.96-.64-.64-.96-1.46-.96-2.47v-89.25c0-1.01.32-1.83.96-2.47.64-.64,1.46-.96,2.47-.96h15.52c1.01,0,1.83.32,2.47.96.64.64.96,1.47.96,2.47v89.25c0,1.01-.32,1.83-.96,2.47-.64.64-1.46.96-2.47.96h-15.52Z'/%3e%3cpath%20class='cls-2'%20d='M1165.57,325.63c-1.01,0-1.83-.32-2.47-.96-.64-.64-.96-1.46-.96-2.47v-89.25c0-1.01.32-1.83.96-2.47.64-.64,1.46-.96,2.47-.96h62.2c1.01,0,1.83.32,2.47.96.64.64.96,1.47.96,2.47v10.99c0,.92-.32,1.69-.96,2.33-.64.64-1.47.96-2.47.96h-44.49v21.56h41.47c1.01,0,1.83.32,2.47.96.64.64.96,1.47.96,2.47v10.16c0,.92-.32,1.7-.96,2.33-.64.64-1.47.96-2.47.96h-41.47v22.25h45.59c1.01,0,1.83.32,2.47.96.64.64.96,1.47.96,2.47v10.85c0,1.01-.32,1.83-.96,2.47-.64.64-1.47.96-2.47.96h-63.3Z'/%3e%3cpath%20class='cls-2'%20d='M1262.7,325.63c-1.01,0-1.83-.32-2.47-.96-.64-.64-.96-1.46-.96-2.47v-89.25c0-1.01.32-1.83.96-2.47.64-.64,1.46-.96,2.47-.96h34.47c9.15,0,16.8,1.44,22.93,4.33,6.13,2.88,10.82,7.1,14.07,12.63,3.25,5.54,4.96,12.38,5.15,20.53.09,4.03.14,7.55.14,10.57s-.05,6.5-.14,10.44c-.27,8.51-1.97,15.56-5.08,21.15-3.11,5.58-7.69,9.73-13.73,12.43-6.04,2.7-13.59,4.05-22.66,4.05h-35.15ZM1281.1,307.92h16.07c4.58,0,8.33-.69,11.26-2.06,2.93-1.37,5.1-3.55,6.52-6.52,1.42-2.97,2.17-6.84,2.27-11.6.09-2.65.16-5.03.21-7.14.04-2.1.04-4.21,0-6.32-.05-2.1-.12-4.44-.21-7-.18-6.87-1.95-11.92-5.29-15.17-3.34-3.25-8.49-4.87-15.45-4.87h-15.38v60.69Z'/%3e%3c/g%3e%3cg%3e%3cpath%20class='cls-2'%20d='M349.9,341c-.98,0-1.87-.4-2.66-1.19-.8-.8-1.19-1.68-1.19-2.66,0-.61.06-1.16.18-1.65l43.18-117.96c.37-1.35,1.13-2.54,2.3-3.58,1.16-1.04,2.78-1.56,4.87-1.56h27.19c2.08,0,3.7.52,4.87,1.56,1.16,1.04,1.93,2.24,2.3,3.58l42.99,117.96c.24.49.37,1.04.37,1.65,0,.98-.4,1.87-1.19,2.66-.8.8-1.75,1.19-2.85,1.19h-22.6c-1.84,0-3.22-.46-4.13-1.38s-1.5-1.75-1.75-2.48l-7.17-18.74h-49.06l-6.98,18.74c-.25.73-.8,1.56-1.65,2.48-.86.92-2.33,1.38-4.41,1.38h-22.6ZM393.08,291.94h34.18l-17.27-48.51-16.9,48.51Z'/%3e%3cpath%20class='cls-2'%20d='M493.39,341c-1.35,0-2.45-.43-3.31-1.29-.86-.86-1.29-1.96-1.29-3.31v-119.43c0-1.35.43-2.45,1.29-3.31s1.96-1.29,3.31-1.29h24.62c1.35,0,2.45.43,3.31,1.29.86.86,1.29,1.96,1.29,3.31v119.43c0,1.35-.43,2.45-1.29,3.31s-1.96,1.29-3.31,1.29h-24.62Z'/%3e%3c/g%3e%3c/svg%3e", t1 = "aiDisclosure", O = "generated", x = "modified";
function c1(e) {
  if (typeof e == "string") return e;
  if (Array.isArray(e) && typeof e[0] == "string") return e[0];
}
function M(e) {
  return c1(
    e?.find((s) => s.alias === t1)?.value
  );
}
function U(e) {
  return e.contentTypeAlias === "Image" && M(e.values) === void 0;
}
var i1 = Object.defineProperty, a1 = Object.getOwnPropertyDescriptor, I = (e) => {
  throw TypeError(e);
}, g = (e, s, t, i) => {
  for (var l = i > 1 ? void 0 : i ? a1(s, t) : s, C = e.length - 1, T; C >= 0; C--)
    (T = e[C]) && (l = (i ? T(s, t, l) : T(l)) || l);
  return i && l && i1(s, t, l), l;
}, E = (e, s, t) => s.has(e) || I("Cannot " + t), c = (e, s, t) => (E(e, s, "read from private field"), s.get(e)), o = (e, s, t) => s.has(e) ? I("Cannot add the same private member more than once") : s instanceof WeakSet ? s.add(e) : s.set(e, t), b = (e, s, t, i) => (E(e, s, "write to private field"), s.set(e, t), t), r = (e, s, t) => (E(e, s, "access private method"), t), l1 = (e, s, t, i) => ({
  set _(l) {
    b(e, s, l);
  },
  get _() {
    return c(e, s);
  }
}), d, w, m, v, h, f, n, D, a, k, Z, $, S, P, A, W, q, L, H, R;
let u = class extends j {
  constructor() {
    super(), o(this, a), this.items = [], this.selectable = !1, this.selection = [], this.itemHrefs = /* @__PURE__ */ new Map(), this.disclosureByUnique = /* @__PURE__ */ new Map(), o(this, d), o(this, w), o(this, m, /* @__PURE__ */ new Set()), o(this, v, /* @__PURE__ */ new Set()), o(this, h, /* @__PURE__ */ new Map()), o(this, f, /* @__PURE__ */ new Map()), o(this, n), o(this, D, 0), this.consumeContext(X, (e) => {
      b(this, d, e), e?.setupView(this), this.observe(
        e?.selection.selectable,
        (s) => this.selectable = s ?? !1,
        "aiDisclosureCollectionSelectableObserver"
      ), this.observe(
        e?.selection.selection,
        (s) => this.selection = s ?? [],
        "aiDisclosureCollectionSelectionObserver"
      ), this.observe(
        e?.items,
        (s) => r(this, a, k).call(this, s),
        "aiDisclosureCollectionItemsObserver"
      );
    }), this.consumeContext(z, (e) => {
      b(this, w, e);
    });
  }
  firstUpdated() {
    r(this, a, Z).call(this);
  }
  connectedCallback() {
    super.connectedCallback(), this.hasUpdated && r(this, a, Z).call(this);
  }
  updated() {
    r(this, a, $).call(this);
  }
  disconnectedCallback() {
    c(this, n)?.disconnect(), b(this, n, void 0);
    for (const e of c(this, h).values()) window.clearTimeout(e);
    c(this, h).clear(), super.disconnectedCallback();
  }
  render() {
    return _`
      <div id="media-grid">
        ${Y(
      this.items,
      (e) => e.unique + ("status" in e ? e.status : ""),
      (e) => r(this, a, L).call(this, e)
    )}
      </div>
    `;
  }
};
d = /* @__PURE__ */ new WeakMap();
w = /* @__PURE__ */ new WeakMap();
m = /* @__PURE__ */ new WeakMap();
v = /* @__PURE__ */ new WeakMap();
h = /* @__PURE__ */ new WeakMap();
f = /* @__PURE__ */ new WeakMap();
n = /* @__PURE__ */ new WeakMap();
D = /* @__PURE__ */ new WeakMap();
a = /* @__PURE__ */ new WeakSet();
k = async function(e) {
  const s = ++l1(this, D)._;
  this.items = e ?? [], r(this, a, W).call(this);
  const t = await Promise.all(
    this.items.map(async (i) => {
      const l = await c(this, d)?.requestItemHref?.(i) ?? N.generateAbsolute({ unique: i.unique });
      return l ? [i.unique, l] : void 0;
    })
  );
  s === c(this, D) && (this.itemHrefs = new Map(
    t.filter((i) => i !== void 0)
  ));
};
Z = function() {
  c(this, n) || (b(this, n, new IntersectionObserver(
    (e) => {
      for (const s of e) {
        if (!s.isIntersecting) continue;
        const t = s.target.dataset.disclosureUnique;
        t && (c(this, n)?.unobserve(s.target), r(this, a, S).call(this, t).then((i) => {
          i || r(this, a, P).call(this, s.target, t);
        }));
      }
    },
    { rootMargin: "200px" }
  )), r(this, a, $).call(this));
};
$ = function() {
  if (c(this, n))
    for (const e of this.renderRoot.querySelectorAll("[data-disclosure-unique]")) {
      const s = e.dataset.disclosureUnique;
      s && !c(this, m).has(s) && !c(this, v).has(s) && c(this, n).observe(e);
    }
};
S = async function(e) {
  if (c(this, m).has(e) || c(this, v).has(e)) return !0;
  c(this, m).add(e);
  try {
    const { data: s, error: t } = await F(
      this,
      G.getMediaById({ path: { id: e } }),
      { disableNotifications: !0 }
    );
    if (t || !r(this, a, A).call(this, e)) return !1;
    c(this, f).delete(e), r(this, a, q).call(this, e, M(s?.values));
    const i = c(this, w)?.byUnique(e);
    return i && (c(this, v).add(e), this.observe(
      i,
      (l) => {
        l && r(this, a, q).call(this, e, M(l.values));
      },
      `aiDisclosureDetailObserver:${e}`
    )), !0;
  } finally {
    c(this, m).delete(e);
  }
};
P = function(e, s) {
  if (c(this, h).has(s)) return;
  const t = (c(this, f).get(s) ?? 0) + 1;
  if (c(this, f).set(s, t), t > 1) return;
  const i = window.setTimeout(() => {
    c(this, h).delete(s), this.isConnected && e.isConnected && r(this, a, A).call(this, s) && c(this, n)?.observe(e);
  }, 2e3);
  c(this, h).set(s, i);
};
A = function(e) {
  const s = this.items.find((t) => t.unique === e);
  return s !== void 0 && U(s);
};
W = function() {
  const e = new Set(
    this.items.filter(U).map((t) => t.unique)
  );
  for (const t of c(this, v))
    e.has(t) || (this.removeUmbControllerByAlias(`aiDisclosureDetailObserver:${t}`), c(this, v).delete(t));
  for (const [t, i] of c(this, h))
    e.has(t) || (window.clearTimeout(i), c(this, h).delete(t));
  for (const t of c(this, f).keys())
    e.has(t) || c(this, f).delete(t);
  const s = new Map(
    [...this.disclosureByUnique].filter(([t]) => e.has(t))
  );
  s.size !== this.disclosureByUnique.size && (this.disclosureByUnique = s);
};
q = function(e, s) {
  if (this.disclosureByUnique.get(e) === s) return;
  const i = new Map(this.disclosureByUnique);
  s ? i.set(e, s) : i.delete(e), this.disclosureByUnique = i;
};
L = function(e) {
  if (e.entityType === V)
    return r(this, a, R).call(this, e);
  const s = this.itemHrefs.get(e.unique), t = r(this, a, H).call(this, e);
  return _`
      <uui-card-media
        name=${p(e.name)}
        data-mark="${e.entityType}:${e.unique}"
        data-disclosure-unique=${p(
    U(e) ? e.unique : void 0
  )}
        ?selectable=${this.selectable}
        ?select-only=${this.selection.length > 0}
        ?selected=${c(this, d)?.selection.isSelected(e.unique) ?? !1}
        href=${p(s)}
        @selected=${() => c(this, d)?.selection.select(e.unique)}
        @deselected=${() => c(this, d)?.selection.deselect(e.unique)}>
        <div class="thumbnail">
          <umb-imaging-thumbnail
            .unique=${e.unique}
            alt=${p(e.name)}
            icon=${p(e.icon)}></umb-imaging-thumbnail>
          ${t ? _`<span class="disclosure-badge" role="img" aria-label=${t.accessibleLabel}>
                <img src=${t.url} alt="" />
              </span>` : ""}
        </div>
        <umb-entity-actions-bundle
          slot="actions"
          .entityType=${e.entityType}
          .unique=${e.unique}></umb-entity-actions-bundle>
      </uui-card-media>
    `;
};
H = function(e) {
  const s = M(e.values) ?? this.disclosureByUnique.get(e.unique);
  if (s === O)
    return { accessibleLabel: O, url: e1 };
  if (s === x)
    return { accessibleLabel: x, url: s1 };
};
R = function(e) {
  const s = e.status === B.COMPLETE, t = e.status !== B.WAITING && !s;
  return _`
      <uui-card-media disabled class="media-placeholder-item" name=${p(e.name)}>
        <umb-temporary-file-badge
          .progress=${e.progress ?? 0}
          ?complete=${s}
          ?error=${t}></umb-temporary-file-badge>
      </uui-card-media>
    `;
};
u.styles = [
  Q,
  K`
      :host {
        display: flex;
        flex-direction: column;
      }

      #media-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
        grid-auto-rows: 200px;
        gap: var(--uui-size-space-5);
      }

      uui-card-media,
      .thumbnail,
      umb-imaging-thumbnail {
        width: 100%;
        height: 100%;
      }

      .thumbnail {
        position: relative;
        overflow: hidden;
      }

      .disclosure-badge {
        position: absolute;
        inset: auto var(--uui-size-space-2)
          calc(var(--uui-size-layout-2) + var(--uui-size-space-2)) auto;
        width: min(7rem, calc(100% - 2 * var(--uui-size-space-2)));
        pointer-events: none;
      }

      .disclosure-badge img {
        display: block;
        width: 100%;
        height: auto;
      }

      umb-entity-actions-bundle {
        --uui-button-background-color: var(--uui-color-surface);
        --uui-button-background-color-hover: var(--uui-color-surface);
      }
    `
];
g([
  y()
], u.prototype, "items", 2);
g([
  y()
], u.prototype, "selectable", 2);
g([
  y()
], u.prototype, "selection", 2);
g([
  y()
], u.prototype, "itemHrefs", 2);
g([
  y()
], u.prototype, "disclosureByUnique", 2);
u = g([
  J("thebuilder-ai-disclosure-media-grid")
], u);
const m1 = u;
export {
  u as AiDisclosureMediaGridElement,
  m1 as default
};
//# sourceMappingURL=ai-disclosure-media-grid.element-ea91XyVE.js.map
